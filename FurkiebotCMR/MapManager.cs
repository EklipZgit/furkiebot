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


namespace MapManager {

    /// <summary>
    /// Denial objects to represent specific instances of denials
    /// </summary>
    class Denial : DBObject {
        public string Message;
        public string Tester;

        public Denial(string theTester, string theMessage) {
            Message = theMessage;
            Tester = theTester;
        }

        public Denial()
            : this("Not Initialized", "Not Initialized") {
        }
    }

    /// <summary>
    /// Class containing all the data about a map.
    /// </summary>
    class CmrMap : DBObject {
        private string name;
        private string nameLower;
        private int atlasId;
        private string filepath;
        private string author;
        private string acceptedBy;
        private bool accepted;
        private string modifiedTimestamp;
        private bool forceid;
        private bool isDenied;
        private int cmrNumber;
        private List<ObjectId> denialMessages;

        public string Name { get { return name; } set { name = value; nameLower = name.ToLower(); } }
        public string NameLower { get { return nameLower; } }
        public int AtlasID {
            get { return atlasId; }
            set {
                if (!forceid) {
                    atlasId = value;
                }
            }
        }
        public string Filepath { get { return filepath; } set { filepath = value; } }
        public string Author { get { return author; } set { author = value; } }
        public bool Accepted { get { return accepted; } set { accepted = value; } }
        public string AcceptedBy { get { return acceptedBy; } set { acceptedBy = value; } }
        public string LastModified { get { return modifiedTimestamp; } set { modifiedTimestamp = value; } }
        public bool IsForcedID { get { return forceid; } }
        public bool IsDenied { get { return isDenied; } set { isDenied = value; } }
        public int CmrNo { get { return cmrNumber; } set { cmrNumber = value; } }



        [BsonConstructor]
        public CmrMap(ObjectId id, bool isForcedId, List<ObjectId> denialMessages) {
            this.Id = id;
            this.denialMessages = denialMessages;
            this.forceid = isForcedId;
        }


        public CmrMap() {
            denialMessages = new List<ObjectId>();
            forceid = false;
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
            
        private FurkieBot fb = FurkieBot.Instance;


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



        public bool Accept(string mapName, string tester, bool isAdmin = false) {
            return Accept(mapName, tester, fb.cmrId, isAdmin);
        }
        public bool Accept(string mapName, string tester, int cmrId, bool isAdmin = false) {
            var selected =
                Maps.AsQueryable<CmrMap>()
                .Where(m => m.NameLower == mapName.Trim().ToLower() && m.CmrNo == cmrId);
            bool successful = false;
            foreach (CmrMap map in selected) {
                if (!map.Accepted) {
                    map.AcceptedBy = tester;
                    map.Accepted = true;
                    successful = true;
                    furkiebot.MsgChans("Map: \"" + map.Name + "\" accepted by ");
                }
            }
            return successful;
        }

        public bool Unaccept(string tester, bool isAdmin = false) {
            if (accepted) {
                acceptedBy = "Not accepted.";
                accepted = false;
                return true;
            } else {
                return false;
            }
        }

        public bool ForceID(string tester, int newId) {
            atlasId = newId;
            forceid = true;
            return true;
        }

        public bool Deny(string tester, ObjectId denialId, bool isAdmin = false) {
            if (!accepted) {
                denialMessages.Add(new Denial(tester, reasonForDenial));
            } else {
                denialMessages.Add(new Denial(tester, reasonForDenial));
                isDenied = Unaccept(tester, isAdmin);
            }
            return isDenied;
        }



        /// <summary>
        /// Writes the given maplist to the file for the provided CMR id number.
        /// </summary>
        /// <param name="maplist">The maplist to write out to disk.</param>
        /// <param name="cmrid">The current cmrid.</param>
        private void WriteMaps() {
            ignoreChangedMaps = true;
            lock (_updatingMapsLock) {
                var mapDict = (mapsTemp == null ? maps : mapsTemp);
                WriteMaps(mapDict, cmrId);
            }
        }



        /// <summary>
        /// Writes the given maplist to the file for the provided CMR id number.
        /// </summary>
        /// <param name="maplist">The maplist to write out to disk.</param>
        /// <param name="cmrid">The current cmrid.</param>
        private void WriteMaps(Dictionary<string, MapData> maplist, int cmrid) {
            string filepath = MAPS_PATH + cmrid + @"\maps.json"; // !! FILEPATH !!

            string json = JsonConvert.SerializeObject(maplist, Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(filepath, json);
        }



        /// <summary>
        /// Deletes a map from the current CMR.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <returns>Whether or not the map deleted successfully.</returns>
        private bool DeleteMap(string mapname, string tester) {
            mapname = mapname.ToLower().Trim();
            Dictionary<string, MapData> maps = (mapsTemp == null ? this.maps : mapsTemp);
            if (maps.ContainsKey(mapname)) {
                if (maps[mapname].accepted) {
                    acceptedCount--;
                } else {
                    pendingCount--;
                }
                maps.Remove(mapname);
                WriteMaps();
                return true;
            }
            return false;
        }


        /// <summary>
        /// Approves a CMR map by name.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <param name="tester">The testers name.</param>
        /// <returns>Whether or not the map existed.</returns>
        private bool ApproveMap(string mapname, string tester) {
            mapname = mapname.ToLower().Trim();
            Dictionary<string, MapData> maps = (mapsTemp == null ? this.maps : mapsTemp);
            if (maps.ContainsKey(mapname) && maps[mapname].accepted == false) {
                MapData map = maps[mapname];
                acceptedCount++;
                pendingCount--;
                map.acceptedBy = tester;
                map.accepted = true;
                maps[mapname] = map;
                WriteMaps(maps, cmrId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Denies the map.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <param name="tester">The tester.</param>
        /// <param name="denyMessage">The deny message.</param>
        /// <returns>Whether or not the map existed.</returns>
        private bool DenyMap(string mapname, string tester, string denyMessage) {
            throw new NotImplementedException();
            return false; //todo
        }



        /// <summary>
        /// Sets the map URL.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        private bool setMapId(string mapname, int id) {
            mapname = mapname.Trim().ToLower();
            if (maps.ContainsKey(mapname)) {
                MapData md = maps[mapname];
                md.id = id;
                md.forceid = true;
                maps[mapname] = md;
                WriteMaps(maps, cmrId);
                return true;
            } else {
                return false;
            }
        }



    }
}