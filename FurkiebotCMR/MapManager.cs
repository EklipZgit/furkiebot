/**
 * MapManager.cs
 * Manager class for the cmr maps in the database.
 * @Author Travis Drake
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver.Wrappers;
//may not need ^

using FurkiebotCMR;
using DatabaseCMR;
using UserCMR;
using AtlasTools;
using System.IO;
using Newtonsoft.Json;
using System.Threading;


namespace MapCMR {
    public class JoinedMapData {
        public Denial denial;
        public CmrMap map;
        public User mapper;
        public User tester;

        public JoinedMapData(CmrMap theMap, Denial theDenial, User theMapper, User theTester) {
            this.denial = theDenial;
            this.map = theMap;
            this.mapper = theMapper;
            this.tester = theTester;
        }
    }


    /// <summary>
    /// Denial objects to represent specific instances of denials
    /// </summary>
    public class Denial : DBObject{
        public string Message;
        public ObjectId TesterId;
        public ObjectId MapId;
        public int Timestamp;
        public bool DisplayedToMapper;
        public int CmrNo;

        [BsonConstructor]
        public Denial(ObjectId id, ObjectId testerId, ObjectId mapId, int cmrNo, string message, int timestamp, bool displayedToMapper) {
            Id = id;
            Message = message;
            TesterId = testerId;
            MapId = mapId;
            Timestamp = timestamp;
            DisplayedToMapper = displayedToMapper;
            CmrNo = cmrNo;
        }

        public Denial() { }
    }

    /// <summary>
    /// Class containing all the data about a map.
    /// </summary>
    public class CmrMap : DBObject{
        private string name;
        private string nameLower;
        private int atlasId;
        private string filepath;
        private ObjectId authorId;
        private ObjectId acceptedById;
        private bool accepted;
        private int modifiedTimestamp;
        private bool isIdForced;
        private bool isDenied;
        private int cmrNumber;

        public string Name { get { return name; } set { name = value; nameLower = name.ToLower(); } }
        public string NameLower { get { return nameLower; } }
        public int AtlasID {
            get { return atlasId; }
            set {
                if (!isIdForced) {
                    atlasId = value;
                }
            }
        }
        public string Filepath { get { return filepath; } set { filepath = value; } }
        public ObjectId AuthorId { get { return authorId; } set { authorId = value; } }
        public bool Accepted { get { return accepted; } set { accepted = value; if (value) { IsDenied = false; } } }
        public ObjectId AcceptedById { get { return acceptedById; } set { acceptedById = value; } }
        public int LastModified { get { return modifiedTimestamp; } set { modifiedTimestamp = value; } }
        public bool IsAtlasIdForced { get { return isIdForced; } }
        public bool IsDenied { get { return isDenied; } set { isDenied = value; } }
        public int CmrNo { get { return cmrNumber; } set { cmrNumber = value; } }



        /// <summary>
        /// Forces the Atlas ID on this map.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <param name="newId">The new Atlas ID.</param>
        /// <returns>Whether or not the map already had a forced ID. (Updates the AtlasID either way)</returns>
        public bool ForceID(string tester, int newId) {
            AtlasID = newId;
            if (!isIdForced) {
                isIdForced = true;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Unforces the Atlas ID on this map.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <returns></returns>
        public bool Unforce(string tester) {
            if (isIdForced) {
                isIdForced = false;
                return true;
            }
            return false;
        }


        [BsonConstructor]
        public CmrMap(ObjectId id, bool isForcedId) {
            Id = id;
            isIdForced = isForcedId;
        }


        public CmrMap() {
            isIdForced = false;
        }

    }


    public class MapManager {
        public const string MONGO_MAP_BACKUP_FILE_NAME = "MongoMapsBackupCmr";
        public const string MONGO_DENIAL_BACKUP_FILE_NAME = "MongoDenialsBackupCmr";
        private static MongoCollection<CmrMap> Maps = DB.Database.GetCollection<CmrMap>(DB._MAP_TABLE_NAME);
        private static MongoCollection<Denial> Denials = DB.Database.GetCollection<Denial>(DB._DENIAL_TABLE_NAME);
        private static MapManager instance;
        //Used to lock the singleton-creation code for this class.
        private static object _instanceLock = new Object();
        private static object _mapfileLock = new Object();
        public static MapManager Instance {
            get {
                lock (_instanceLock) {
                    if (instance == null) {
                        instance = new MapManager();
                    }
                }
                return instance;
            }
        }
            
        private FurkieBot fb = FurkieBot.Instance;
        private UserManager UserMan = UserManager.Instance;


        private IQueryable<CmrMap> lastMaps;
        private IQueryable<Denial> lastDenials;
        private FileSystemWatcher mapsWatcher;

        private MapManager() {
            //register the mapping between the CmrMap class and MongoDB documents.
            if (!BsonClassMap.IsClassMapRegistered(typeof(CmrMap))) {
                BsonClassMap.RegisterClassMap<CmrMap>(cm => {
                    //for private stuff, have constructor with below form
                    cm.AutoMap();
                    //cm.MapCreator(p => new CmrMap(p.IsForcedID, p.denialList));

                    //set ID
                    //cm.MapIdProperty(c => c.);
                    // mappings for other fields and properties


                    //cm.MapProperty(c => c.SomeProperty);
                    //cm.MapProperty(c => c.AnotherProperty);
                });
            }
            //register the mapping between the Denial class and MongoDB documents.
            if (!BsonClassMap.IsClassMapRegistered(typeof(Denial))) {
                BsonClassMap.RegisterClassMap<Denial>(cm => {
                    cm.AutoMap();
                });
            }


            mapsWatcher = new FileSystemWatcher();
            mapsWatcher.Path = FurkieBot.MAPS_UPDATE_PATH;
            mapsWatcher.NotifyFilter = NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            //register CheckMaps to be called any time the MAPS_UPDATE_PATH is modified. 
            //(this file is simply used to notify MapManager that it needs to recheck the mongoDB database,
            //since you cant register events to watch a mongoDB).
            mapsWatcher.Changed += new FileSystemEventHandler(CheckMaps);
            mapsWatcher.EnableRaisingEvents = true;
            
            lastMaps = DeserializeMaps(fb.cmrId);
            lastDenials = DeserializeDenials(fb.cmrId);
            CheckMaps();
        }


		/// <summary>
		/// Gets a <see cref="CmrMap"/> by object identifier.
		/// </summary>
		/// <param name="id">The identifier of the map to retrieve.</param>
		/// <returns>The map by that ID. Null if non-existent.</returns>
		public CmrMap GetMapByObjectId(ObjectId id) {
			return Maps.AsQueryable<CmrMap>()
				.Where<CmrMap>(u => u.Id == id)
				.First<CmrMap>();
		}


        /// <summary>
        /// Indexer by ObjectId, just calls GetMapByObjectId(id);
        /// </summary>
        /// <param name="id">The identifier of the map to find.</param>
        /// <returns>The Map with that ID. Null if non-existent</returns>
        public CmrMap this[ObjectId id] {
            get {
				return GetMapByObjectId(id);
            }
        }


		/// <summary>
		/// Gets a <see cref="CmrMap"/> by name.
		/// </summary>
		/// <param name="name">The name of the map to retrieve.</param>
		/// <returns>The first map result.</returns>
		public CmrMap GetMapByName(string name) {
			return Maps.AsQueryable()
				.Where(u => u.NameLower == name.ToLower().Trim() && u.CmrNo == fb.cmrId)
				.First();				
		}

        /// <summary>
        /// Indexer by string (mapname), just calls GetMapByName(name);
        /// </summary>
        /// <param name="name">The name of the map to find</param>
        /// <returns>The Map by that name. Null if non-existent</returns>
        public CmrMap this[string name] {
            get {
				return GetMapByName(name);
            }
        }


        /// <summary>
        /// Gets all denials, including those from previous CMR's denials.
        /// </summary>
        /// <returns>All denials ever</returns>
        public IQueryable<Denial> GetAllDenials() {
			return Denials.AsQueryable<Denial>().OrderBy<Denial, int>(c => c.Timestamp);
        }


        /// <summary>
        /// Gets the denials for the current CMR.
        /// </summary>
        /// <returns>All denials in the current CMR</returns>
        public IQueryable<Denial> GetDenials() {
            return GetDenials(fb.cmrId);
        }


        /// <summary>
        /// Gets the denials for the provided CMR number.
        /// </summary>
        /// <param name="cmrId">The CMR identifier.</param>
        /// <returns>
        /// All denials in the provided CMR
        /// </returns>
        public IQueryable<Denial> GetDenials(int cmrId) {
            return Denials.AsQueryable<Denial>().Where(c => c.CmrNo == cmrId).OrderBy<Denial, int>(c => c.Timestamp);
        }


        /// <summary>
        /// Gets the denial by identifier.
        /// </summary>
        /// <param name="denialId">The denial identifier.</param>
        /// <returns></returns>
        public Denial GetDenialById(ObjectId denialId) {
            return Denials.AsQueryable<Denial>().Where(c => c.Id == denialId).First();
        }


        /// <summary>
        /// Gets the denials by map ID.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <returns></returns>
        public IQueryable<Denial> GetDenialsByMap(ObjectId mapId) {
            return Denials.AsQueryable<Denial>().Where(c => c.MapId == mapId).OrderBy<Denial, int>(c => c.Timestamp);
        }


        /// <summary>
        /// Gets the most recent denial for the provided MapId.
        /// </summary>
        /// <param name="mapId">The map identifier.</param>
        /// <returns></returns>
        public Denial GetLatestDenialByMap(ObjectId mapId) {
            return Denials.AsQueryable<Denial>().Where(c => c.MapId == mapId).OrderBy<Denial, int>(c => c.Timestamp).First();
        }


        /// <summary>
        /// Gets the maps for the current CMR
        /// </summary>
        /// <returns>The maps for the current cmr, sorted by modified timestamp</returns>
        public IQueryable<CmrMap> GetMaps() { return GetMaps(fb.cmrId); }
        /// <summary>
        /// Gets the maps.
        /// </summary>
        /// <param name="cmrId">The CMR Number.</param>
        /// <returns></returns>
        public IQueryable<CmrMap> GetMaps(int cmrId) {
            return Maps.AsQueryable<CmrMap>()
                .Where(m => m.CmrNo == cmrId)
                .OrderBy<CmrMap, int>(c => c.LastModified);
        }


        /// <summary>
        /// Gets the accepted maps.
        /// </summary>
        /// <returns></returns>
        public IQueryable<CmrMap> GetAcceptedMaps() { return GetAcceptedMaps(fb.cmrId); }
        /// <summary>
        /// Gets the accepted maps.
        /// </summary>
        /// <param name="cmrId">The CMR Number.</param>
        /// <returns></returns>
        public IQueryable<CmrMap> GetAcceptedMaps(int cmrId) {
            return Maps.AsQueryable<CmrMap>()
                .Where(m => m.CmrNo == cmrId && m.Accepted == true)
                .OrderBy<CmrMap, int>(c => c.LastModified);
        }


        /// <summary>
        /// Gets the pending maps.
        /// </summary>
        /// <returns></returns>
        public IQueryable<CmrMap> GetPendingMaps() { return GetPendingMaps(fb.cmrId); }
        /// <summary>
        /// Gets the pending maps.
        /// </summary>
        /// <param name="cmrId">The CMR Number.</param>
        /// <returns></returns>
        public IQueryable<CmrMap> GetPendingMaps(int cmrId) {
            return Maps.AsQueryable<CmrMap>()
                .Where(m => m.CmrNo == cmrId && m.Accepted == false && m.IsDenied == false)
                .OrderBy<CmrMap, int>(c => c.LastModified);
        }


        /// <summary>
        /// Gets the denied maps.
        /// </summary>
        /// <returns></returns>
        public IQueryable<CmrMap> GetDeniedMaps() { return GetDeniedMaps(fb.cmrId); }
        /// <summary>
        /// Gets the denied maps.
        /// </summary>
        /// <param name="cmrId">The CMR Number.</param>
        /// <returns></returns>
        public IQueryable<CmrMap> GetDeniedMaps(int cmrId) {
            return Maps.AsQueryable<CmrMap>()
                .Where(m => m.CmrNo == cmrId && m.Accepted == false && m.IsDenied == true)
                .OrderBy<CmrMap, int>(c => c.LastModified);
        }


        


        /// <summary>
        /// Saves the provided map to the Database.
        /// </summary>
        /// <param name="map">The map to save</param>
        /// <returns>Whether or not the save was successful ?</returns>
        public bool SaveMap(CmrMap map) {
            var result = Maps.Save(map);
            return result.DocumentsAffected > 0;
        }



        /// <summary>
        /// Saves the provided Denial object to the Database.
        /// </summary>
        /// <param name="denial">The Denial instance to save</param>
        /// <returns>Whether or not the save was successful ?</returns>
        public bool SaveDenial(Denial denial) {
            var result = Denials.Save(denial);
            return result.DocumentsAffected > 0;
        }



        /// <summary>
        /// Accepts the specified map for the current CMR.
        /// </summary>
        /// <param name="mapName">Name of the map.</param>
        /// <param name="tester">The tester.</param>
        /// <param name="isAdmin">if set to <c>true</c> [is admin].</param>
        /// <returns>Whether or not the map was accepted.</returns>
        public bool Accept(string mapName, string tester, bool isAdmin = false) {
            return Accept(mapName, tester, fb.cmrId, isAdmin);
        }
        /// <summary>
        /// Accepts the specified map for the current CMR.
        /// </summary>
        /// <param name="mapName">Name of the map.</param>
        /// <param name="tester">The tester.</param>
        /// <param name="cmrId">The cmr for which to accept the map.</param>
        /// <param name="isAdmin">if set to <c>true</c> [is admin].</param>
        /// <returns>Whether or not the map was accepted.</returns>
        public bool Accept(string mapName, string tester, int cmrId, bool isAdmin = false) {
            var selected =
                Maps.AsQueryable<CmrMap>()
                .Where(m => m.NameLower == mapName.Trim().ToLower() && m.CmrNo == cmrId);
            bool successful = false;
            foreach (CmrMap map in selected) {
                if (!map.Accepted) {
                    User testerUsr = UserMan[tester];
                    if (testerUsr != null) {
                        map.AcceptedById = testerUsr.Id;
                        map.Accepted = true;
                        successful = true;
                        fb.MsgChans("Map: \"" + map.Name + "\" accepted by " + tester);
                        SaveMap(map);
                    }
                }
            }
            return successful;
        }



        /// <summary>
        /// Unaccepts the specified map.
        /// </summary>
        /// <param name="mapName">Name of the map.</param>
        /// <param name="tester">The tester.</param>
        /// <param name="denialMessage">The denial message.</param>
        /// <param name="isAdmin">if set to <c>true</c> [is admin].</param>
        /// <returns></returns>
        public bool Unaccept(string mapName, string tester, string denialMessage = null, bool isAdmin = false) {

            var selected = this[mapName];
            bool successful = false;
            if (selected.Accepted) {
                User testerUsr = UserMan[tester];
                if (testerUsr != null) {
                    selected.Accepted = false;
                    if (denialMessage != null) {
                        selected.IsDenied = true;
                        Denial denial = new Denial();
                        denial.MapId = selected.Id;
                        denial.TesterId = testerUsr.Id;
                        denial.Message = denialMessage;
                        denial.Timestamp = DB.UnixTimestamp;
                        SaveDenial(denial);
                    }
                    successful = true;
                    fb.MsgChans("Map: \"" + selected.Name + "\" accepted by " + tester);
                    SaveMap(selected);
                }
            }
            return successful;
        }

        /// <summary>
        /// Forces the maps atlas identifier.
        /// </summary>
        /// <param name="mapName">Name of the map.</param>
        /// <param name="tester">The tester.</param>
        /// <param name="newId">The new AtlasID.</param>
        /// <returns>Whether or not the map was found.</returns>
        public bool ForceMapAtlasID(string mapName, string tester, int newId) {
            CmrMap map = this[mapName];
            if (map != null) {
                map.ForceID(tester, newId);
                return true;
            }
            return false;
        }


        /// <summary>
        /// Unforces the Atlas ID on this map.
        /// </summary>
        /// <param name="tester">The tester.</param>
        /// <returns>Whether or not the map was found.</returns>
        public bool UnforceMapAtlasID(string mapName, string tester) {
            CmrMap map = this[mapName];
            if (map != null) {
                map.Unforce(tester);
                return true;
            }
            return false;
        }



        /// <summary>
        /// Denies the map with the provided map name.
        /// </summary>
        /// <param name="mapName">Name of the map.</param>
        /// <param name="tester">The tester.</param>
        /// <param name="denialMessage">The denial message.</param>
        /// <param name="isAdmin">if set to <c>true</c> [is admin].</param>
        /// <returns>Whether or not the map was successfully denied.</returns>
        public bool DenyMap(string mapName, string tester, string denialMessage, bool isAdmin = false) {
            var selected = this[mapName];
            bool successful = false;
            if (!selected.Accepted) {
                User testerUsr = UserMan[tester];
                if (testerUsr != null) {
                    selected.Accepted = false;
                    if (denialMessage != null) {
                        selected.IsDenied = true;
                        Denial denial = new Denial();
                        denial.MapId = selected.Id;
                        denial.TesterId = testerUsr.Id;
                        denial.Message = denialMessage;
                        denial.Timestamp = DB.UnixTimestamp;
                        SaveDenial(denial);
                    }
                    successful = true;
                    fb.MsgChans("Map: \"" + selected.Name + "\" accepted by " + tester);
                    SaveMap(selected);
                }
            } else {
                fb.Notice(tester, "Map couldn't be denied because it has already been accepted. Instead, use:");
                fb.Notice(tester, ".unaccept \"mapname\" denial message");
            }
            return successful;
        }



        /// <summary>
        /// Deletes a map from the current CMR.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <returns>Whether or not the map deleted successfully.</returns>
        public bool DeleteMap(string mapname, string tester) {
            return DeleteMap(mapname, tester, fb.cmrId);
        }
        /// <summary>
        /// Deletes a map from the current CMR.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <returns>Whether or not the map deleted successfully.</returns>
        public bool DeleteMap(string mapname, string tester, int cmrId) {
            var query = Query.And(Query.EQ("NameLower", mapname.Trim().ToLower()),
                                  Query.EQ("CmrNo", cmrId));
            var removing = Maps.Find(query);
            var results = Maps.Remove(query);
            foreach (CmrMap map in removing) {
                var linqQuery = Query<Denial>.Where(d => d.MapId == map.Id);
                if (Denials.Remove(linqQuery).DocumentsAffected > 0) { Console.WriteLine("Successfully removed a map Denial for map: " + map.Name); }
            }
            return results.DocumentsAffected > 0;
        }




        /// <summary>
        /// Sets the map URL.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public bool SetMapId(string mapname, int id) {
            CmrMap map = this[mapname];
            if (map != null) {
                map.AtlasID = id;
                SaveMap(map);
                return true;
            } 
            return false;
        }



        /// <summary>
        /// Gets the joined map data, useful for not having to manually join later.
        /// </summary>
        /// <returns>List of joined map data, sorted by map name.</returns>
        public List<JoinedMapData> GetJoinedMapData() {
            var maps = Maps.AsQueryable<CmrMap>()
                .Where(m => m.CmrNo == fb.cmrId)
                .OrderBy<CmrMap, ObjectId>(m => m.Id);
            var denials = Maps.AsQueryable<Denial>()
                .Where(m => m.CmrNo == fb.cmrId)
                .OrderBy<Denial, ObjectId>(m => m.MapId);
            var users = UserMan.GetUsers();
            var joined =
                from map in maps
                join denial in denials on map.Id equals denial.MapId
                join mapper in users on map.AuthorId equals mapper.Id
                join tester in users on denial.TesterId equals tester.Id
                orderby map.Name
                select new JoinedMapData(map, denial, mapper, tester);
            return joined.ToList();
        }







        /// <summary>
        /// Writes the current maps and denials to the temp mapfile and denialfile. 
        /// Used so FurkieBot can notify about changes that happened while he was offline.
        /// </summary>
        private void BackupMapState() { //TODO no idea if this works....
            var maps = Maps.AsQueryable<CmrMap>()
                .Where(m => m.CmrNo == fb.cmrId)
                .OrderBy<CmrMap, ObjectId>(m => m.Id);
            var denials = Maps.AsQueryable<Denial>()
                .Where(m => m.CmrNo == fb.cmrId)
                .OrderBy<Denial, ObjectId>(m => m.MapId);
            WriteMaps(maps, fb.cmrId);
            WriteDenials(denials, fb.cmrId);
        }



        /// <summary>
        /// Writes the given maps to the temp map file for the provided CMR id number.
        /// Used so FurkieBot can notify about changes that happened while he was offline.
        /// </summary>
        /// <param name="maps">The maps to write out to disk.</param>
        /// <param name="cmrid">The current cmrid.</param>
        private void WriteMaps(IQueryable<CmrMap> maps, int cmrid) {
            List<CmrMap> mapList = maps.ToList();
            string filepath = FurkieBot.MAPS_PATH + MONGO_MAP_BACKUP_FILE_NAME + cmrid + ".json"; // !! FILEPATH !!

            string json = JsonConvert.SerializeObject(mapList, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filepath, json);
        }



        /// <summary>
        /// Writes the given denials to the temp denial file for the provided CMR id number.
        /// Used so FurkieBot can notify about changes that happened while he was offline.
        /// </summary>
        /// <param name="denials">The denials to write out to disk.</param>
        /// <param name="cmrid">The current cmrid.</param>
        private void WriteDenials(IQueryable<Denial> denials, int cmrid) {
            List<Denial> denialList = denials.ToList();
            string filepath = FurkieBot.MAPS_PATH + MONGO_DENIAL_BACKUP_FILE_NAME + cmrid + ".json"; // !! FILEPATH !!

            string json = JsonConvert.SerializeObject(denialList, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filepath, json);
        }








        /// <summary>
        /// Returns a copy of the last serialized mongo collection
        /// </summary>
        /// <param name="cmrid">The CMR identifier.</param>
        /// <returns>The loaded map list. If the file doesnt exist, returns an empty maplist.</returns>
        private IQueryable<CmrMap> DeserializeMaps(int cmrid) {

            string mapsJsonPath = FurkieBot.MAPS_PATH + MONGO_MAP_BACKUP_FILE_NAME + cmrid + ".json";
            int limit = 100;
            int cur = 0;
            if (File.Exists(mapsJsonPath)) {
                while (true) { // loop waiting for file unlock
                    cur++;
                    if (cur > limit) {
                        throw new Exception("Looped too long trying to load maps. Map file locked for too long?");
                    }
                    try {
                        Console.WriteLine("Trying to load map file.");
                        string[] mapsarray = File.ReadAllLines(mapsJsonPath);
                        string jsonPending = string.Join("", mapsarray);
                        return JsonConvert.DeserializeObject<List<CmrMap>>(jsonPending).AsQueryable<CmrMap>(); // initially loads the userlist from JSON
                    } catch (System.IO.IOException e) {
                        Console.WriteLine("Got an error trying to read file. Error: ");
                        Console.WriteLine(e.StackTrace);
                    }
                    Thread.Sleep(5);
                }
            } else {
                return null;
            }
        }








        /// <summary>
        /// Returns a copy of the last serialized mongo collection
        /// </summary>
        /// <param name="cmrid">The CMR identifier.</param>
        /// <returns>The loaded map list. If the file doesnt exist, returns an empty maplist.</returns>
        private IQueryable<Denial> DeserializeDenials(int cmrid) {

            string mapsJsonPath = FurkieBot.MAPS_PATH + MONGO_DENIAL_BACKUP_FILE_NAME + cmrid + ".json";
            int limit = 100;
            int cur = 0;
            if (File.Exists(mapsJsonPath)) {
                while (true) { // loop waiting for file unlock
                    cur++;
                    if (cur > limit) {
                        throw new Exception("Looped too long trying to load maps. Map file locked for too long?");
                    }
                    try {
                        Console.WriteLine("Trying to load map file.");
                        string[] mapsarray = File.ReadAllLines(mapsJsonPath);
                        string jsonPending = string.Join("", mapsarray);
                        return JsonConvert.DeserializeObject<List<Denial>>(jsonPending).AsQueryable<Denial>(); // initially loads the userlist from JSON
                    } catch (System.IO.IOException e) {
                        Console.WriteLine("Got an error trying to read file. Error: ");
                        Console.WriteLine(e.StackTrace);
                    }
                    Thread.Sleep(5);
                }
            } else {
                return null;
            }
        }



		/// <summary>
		/// Just calls the parameterized version of the method with nulls. <see mref="CheckMaps(object sender, FileSystemEventArgs e)"/>
		/// </summary>
        private void CheckMaps() {
            CheckMaps(null, null);
        }
		/// <summary>
		/// A beautiful function to check the state of the database. Or something. Its terrible.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="FileSystemEventArgs"/> instance containing the event data.</param>
        private void CheckMaps(object sender, FileSystemEventArgs e) {
            lock (_mapfileLock) {
                var oldMaps = lastMaps;
                var curMaps = GetMaps();

                var oldDenials = lastDenials;
                var curDenials = GetDenials();

                if (lastMaps != null && lastMaps.Count() > 0) {
                    //ensure it wasnt a blank stored file.
                    int acceptedCount = 0;
                    int pendingCount = 0;
                    int deniedCount = 0;
                    string message;
                    HashSet<CmrMap> oldMapSet = new HashSet<CmrMap>();
                    foreach(CmrMap oldMap in oldMaps) {
                        oldMapSet.Add(oldMap);
                    }
                    foreach (CmrMap curMap in curMaps) {     
						// Notifies IRC about any state changes that have happened.
                        CmrMap oldMap = oldMaps.Where(m => m.Id == curMap.Id).First();

                        IQueryable<Denial> curMapDenials = curDenials.Where(d => d.MapId == curMap.Id).OrderBy<Denial, int>(d => d.Timestamp);
                        IQueryable<Denial> oldMapDenials = oldDenials.Where(d => d.MapId == oldMap.Id).OrderBy<Denial, int>(d => d.Timestamp);
                        if (oldMap != null) {
                            oldMapSet.Remove(oldMap);
                            if (curMap.Accepted) {
                                acceptedCount++;
                                if (!oldMap.Accepted) {
                                    //Map was just approved.
                                    message = "Map \"" + curMap.Name + "\" by " + UserMan[curMap.AuthorId].Name + " approved by " + UserMan[curMap.AcceptedById];
                                    fb.MsgChans(message);
                                }
                            } else if (oldMap.LastModified != curMap.LastModified) {
                                message = "Map \"" + curMap.Name + "\" by " + UserMan[curMap.AuthorId].Name
                                        + (curMap.LastModified == oldMap.LastModified ? " is no longer an approved map." : " has been resubmitted and must be re-tested");
                                fb.MsgChans(message);
                                fb.MsgTesters(message + ". Download " + GetDownloadLink(curMap.Name) + FurkieBot.SEP + " Approve at " + FurkieBot.TEST_LINK);

                            } else {
                                pendingCount++;
                                if (oldMap.Accepted && !curMap.IsDenied) {
                                    //Accepted map was resubmitted
                                    message = "Map \"" + curMap.Name + "\" by " + UserMan[curMap.AuthorId].Name +
                                        (curMap.LastModified == oldMap.LastModified ? " is no longer an approved map." : " has been resubmitted and must be re-tested");
                                    fb.MsgChans(message);
                                    fb.MsgTesters(message + ". Download " + GetDownloadLink(curMap.Name) + FurkieBot.SEP + " Approve at " + FurkieBot.TEST_LINK);
                                }
                            }
                            if (curMap.IsDenied) {
                                deniedCount++;
                                if (!oldMap.IsDenied) {
                                    //Map has been denied, find reason.
                                    //TODO linq to find max denial timestamp ??
                                    foreach (Denial curDenial in curMapDenials.OrderBy(d => d.Timestamp)) {
                                        var matches =
                                            from oldDenial in oldDenials
                                            where oldDenial.Id == curDenial.Id
                                            select oldDenial;
                                        if (matches.Count() == 0) {
                                            //then this denial is the cause for the new denial
                                            User mapper = UserMan[curMap.AuthorId];
                                            fb.Msg(mapper.Name, "Your map, \"" + curMap.Name + "\" has been denied for the following reason:  "
                                                + FurkieBot.BoldText(curDenial.Message) + FurkieBot.SEP
                                                + "Please fix the issue and reupload the fixed map to the CMR site. "
                                                + FurkieBot.BoldText("Make sure to submit with the same map name."));

                                            if (fb.IsIdentified(mapper.Name)) {
                                                //mapper is guaranteed to be in IRC
                                                curDenial.DisplayedToMapper = true;
                                                SaveDenial(curDenial);
                                            }
                                        } else if (matches.First().Timestamp != curDenial.Timestamp) {
                                            Console.WriteLine("Denial modified, but not notifying ????");
                                        }
                                    }
                                }
                            } else {
                                //map not currently denied
                                if (oldMap.IsDenied) {
                                    // map has been approved, should already be handled
                                }
                            }
                        } else {
                            //oldMap was null, curMap is newly submitted 
                            if (curMap.Accepted) {
                                acceptedCount++;
                                Console.WriteLine("Wtf new map added and already accepted????");
                                
                                message = "Map \"" + curMap.Name + "\" by " + UserMan[curMap.AuthorId] + " submitted for testing";
                                fb.MsgChans(message);
                                fb.MsgTesters(message + ". Download " + GetDownloadLink(curMap.Name) + FurkieBot.SEP + " Approve at " + FurkieBot.TEST_LINK);
                            } else {

                            }
                            if (curMap.IsDenied) {
                                deniedCount++;
                            }
                        }
                        
                    }
                    if (oldMapSet.Count() > 0) {
                        //Maps remaining in the set were deleted, since all the maps in curMaps have been removed from this set.
                        message = "Map removed from this CMR: ";
                        foreach (CmrMap deleted in oldMapSet) {
                            message += "\"" + deleted.Name + "\" by " + UserMan[deleted.AuthorId] + FurkieBot.SEP;
                        }
                        message = message.Substring(0, message.Length - 3);
                        fb.MsgChans(message);
                        fb.MsgTesters(message);
                    }
                } else { // set last state to the current one.
                    lastMaps = GetMaps();
                    lastDenials = GetDenials();
                    WriteDenials(lastDenials, fb.cmrId);
                    WriteMaps(lastMaps, fb.cmrId);
                }
            } //end lock
        }


        /// <summary>
        /// Gets the download link for the specified map name.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <returns></returns>
        public static string GetDownloadLink(string mapname) {
            return FurkieBot.DOWNLOAD_LINK + mapname.Replace(" ", "%20");
        }
    }
}