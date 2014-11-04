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
using FurkieDB;
using CmrUser;


namespace CmrMap {

    /// <summary>
    /// Denial objects to represent specific instances of denials
    /// </summary>
    class Denial : DBObject {
        public string Message;
        public ObjectId TesterId;
        public ObjectId MapId;
        public int Timestamp;
        public bool DisplayedToMapper;

        [BsonConstructor]
        public Denial(ObjectId id, ObjectId testerId, ObjectId mapId, string message, int timestamp, bool displayedToMapper) {
            Id = id;
            Message = message;
            TesterId = testerId;
            MapId = mapId;
            Timestamp = timestamp;
            DisplayedToMapper = displayedToMapper;
        }
        
        public Denial() {}
    }

    /// <summary>
    /// Class containing all the data about a map.
    /// </summary>
    class CmrMap : DBObject {
        private string name;
        private string nameLower;
        private int atlasId;
        private string filepath;
        private ObjectId authorId;
        private ObjectId acceptedById;
        private bool accepted;
        private string modifiedTimestamp;
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
        public string LastModified { get { return modifiedTimestamp; } set { modifiedTimestamp = value; } }
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
    
        
    class MapManager {
        private static MongoCollection<CmrMap> Maps = DB.Database.GetCollection<CmrMap>(DB._MAP_TABLE_NAME);
        private static MongoCollection<Denial> Denials = DB.Database.GetCollection<Denial>(DB._DENIAL_TABLE_NAME);
        private static MapManager instance;
        public static MapManager Instance {
            get {
                if (instance == null) {
                    instance = new MapManager();
                }
                return instance;
            }
        }
        public static Int32 UnixTimestamp { get { return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; } }
            
        private FurkieBot fb = FurkieBot.Instance;
        private UserManager UserMgr = UserManager.Instance;


        private MapManager() {
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
            if (!BsonClassMap.IsClassMapRegistered(typeof(Denial))) {
                BsonClassMap.RegisterClassMap<Denial>(cm => {
                    cm.AutoMap();
                });
            }
        }



        /// <summary>
        /// Gets the <see cref="CmrMap"/> with the specified identifier from the Maps Mongo Collection
        /// </summary>
        /// <param name="id">The identifier of the map to find.</param>
        /// <returns>The Map with that ID. Null if non-existent</returns>
        public CmrMap this[ObjectId id] {
            get {
                return Maps.AsQueryable<CmrMap>()
                    .Where<CmrMap>(u => u.Id == id)
                    .First<CmrMap>();
            }
        }


        /// <summary>
        /// Gets the <see cref="CmrMap"/> with the specified name out of the Maps mongocollection.
        /// </summary>
        /// <param name="name">The name of the map to find</param>
        /// <returns>The Map by that name. Null if non-existent</returns>
        public CmrMap this[string name] {
            get {
                return Maps.AsQueryable()
                    .Where(u => u.NameLower == name.ToLower().Trim() && u.CmrNo == fb.cmrId)
                    .First();
            }
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
                    User testerUsr = UserMgr[tester];
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
                User testerUsr = UserMgr[tester];
                if (testerUsr != null) {
                    selected.Accepted = false;
                    if (denialMessage != null) {
                        selected.IsDenied = true;
                        Denial denial = new Denial();
                        denial.MapId = selected.Id;
                        denial.TesterId = testerUsr.Id;
                        denial.Message = denialMessage;
                        denial.Timestamp = UnixTimestamp;
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
                User testerUsr = UserMgr[tester];
                if (testerUsr != null) {
                    selected.Accepted = false;
                    if (denialMessage != null) {
                        selected.IsDenied = true;
                        Denial denial = new Denial();
                        denial.MapId = selected.Id;
                        denial.TesterId = testerUsr.Id;
                        denial.Message = denialMessage;
                        denial.Timestamp = UnixTimestamp;
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
        private bool DeleteMap(string mapname, string tester) {
            return DeleteMap(mapname, tester, fb.cmrId);
        }
        /// <summary>
        /// Deletes a map from the current CMR.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <returns>Whether or not the map deleted successfully.</returns>
        private bool DeleteMap(string mapname, string tester, int cmrId) {
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
        private bool SetMapId(string mapname, int id) {
            CmrMap map = this[mapname];
            if (map != null) {
                map.AtlasID = id;
                SaveMap(map);
                return true;
            } 
            return false;
            
        }
    }
}