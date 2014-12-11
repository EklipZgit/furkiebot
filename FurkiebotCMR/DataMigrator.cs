using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DatabaseCMR;
using System.IO;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using MapCMR;
using UserCMR;

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
using System.Text.RegularExpressions;

namespace FurkiebotCMR
{
    /// <summary>
    /// Class to migrate old filesystem stored data to MongoDB.
    /// </summary>
    class DataMigrator
    {
        struct MapData
        {
            public string name;
            public int id;
            public string filepath;
            public string author;
            public string acceptedBy;
            public bool accepted;
            public string timestamp;
            public bool forceid;
        }


        /// <summary>
        /// Player info struct containing all information about a player.
        /// </summary>
        struct PlayerInfo
        {
            public string ircname;
            public string dustforcename;
            public string streamurl;
            public bool tester;
            public bool trusted;
            public bool admin;
            public bool notify;
            public int rating;
            public int randmaprating;
            public string password;
            public string salt;
        }


        /// <summary>
        /// Initializes the database from the deprecated method of storage in FurkieBot v1. Loads users and maps from the UserList and Maps folders and puts their data into a mongo DB.
        /// </summary>
        public static void InitDBFromLocalFiles() {
            Console.WriteLine("Attempting to initialize the database from deprecated local files.");
            MongoDatabase db = DB.Database;
            if (DB._DB_NAME == DB._PRODUCTION_DB_NAME_DO_NOT_USE) {
                Console.WriteLine("HOLY SHIT YOU'RE ABOUT TO OVERWRITE PRODUCTION DB WITH WHATEVERS IN THOSE FILES! ARE YOU SURE? (y/n)");
                string input = Console.ReadLine();
                if (input.Trim().ToLower() == "y") {

                    Console.WriteLine("ARE YOU /REALLY/ SURE?????? (y/n)");
                    input = Console.ReadLine();
                    if (input.Trim().ToLower() == "y") {
                        Console.WriteLine("Ok");
                    } else {
                        throw new Exception("Quitting in a very non-graceful way because I dont remember how to System.Exit() in C#.");
                    }
                } else {
                    throw new Exception("Quitting in a very non-graceful way because I dont remember how to System.Exit() in C#.");
                }
            }
            #region clear the database.
            var collectionNames = db.GetCollectionNames();
            Console.WriteLine("Dropping collections....");
            foreach (string name in collectionNames) {
                if (name != "system.indexes") {
                    Console.WriteLine("Dropping collection: " + name);
                    db.DropCollection(name);
                }
            }
            Console.Write("Completed collection dropping. Collections now: ");
            collectionNames = db.GetCollectionNames();
            foreach (string name in collectionNames) {
                Console.WriteLine(name);
            }
            Console.WriteLine("Collections dropped.\n");
            #endregion

            UserManager UserMan = UserManager.Instance;
            MapManager MapMan = MapManager.Instance;


            #region Load Users
            string userbasepath = FurkieBot.USERLIST_PATH;
            string userJson = File.ReadAllText(userbasepath);
            Dictionary<string, PlayerInfo> oldUsers = JsonConvert.DeserializeObject<Dictionary<string, PlayerInfo>>(userJson);
            foreach (KeyValuePair<string, PlayerInfo> entry in oldUsers) {
                PlayerInfo oldUser = entry.Value;
                User newUser = new User();

                newUser.Admin = oldUser.admin;
                newUser.Name = oldUser.ircname;
                newUser.Notify = oldUser.notify;
                newUser.Password = oldUser.password;
                newUser.Salt = oldUser.salt;
                newUser.Streamurl = oldUser.streamurl;
                newUser.Tester = oldUser.tester;
                newUser.Rating = oldUser.rating;
                newUser.RandmapRating = oldUser.randmaprating;
                newUser.Trusted = oldUser.trusted;
                UserMan.AddUser(newUser);
                Console.WriteLine("Added user: " + newUser.ToString());
            }
            #endregion
            UserMan.DumpStateToFile(@"C:\CMR\MongoUserDumpAfterMigrateAdd.txt");

            #region Load Maps
            string basepath = FurkieBot.MAPS_PATH.ToLower();
            string[] mapfolders = Directory.GetDirectories(basepath);
            foreach (string s in mapfolders)
            {
                string filename = s + "\\maps.json";
                try {
                    string filetext = File.ReadAllText(filename);
                    string cmrNumber = (s.ToLower().Replace(basepath, ""));
                    int cmrInt = int.Parse(Regex.Split(cmrNumber, "/D")[0]);
                    Console.WriteLine("Attempting to load the maps for cmr number: " + cmrInt);
                    Dictionary<string, MapData> oldMaps = JsonConvert.DeserializeObject<Dictionary<string, MapData>>(filetext); //TODO real old file name
                    foreach (KeyValuePair<string, MapData> entry in oldMaps) {
                        CmrMap newMap = new CmrMap();
                        MapData oldMap = entry.Value;

                        newMap.CmrNo = cmrInt;

                        User tester = UserMan[oldMap.acceptedBy];
                        if (tester == null) {
                            tester = UserMan["EklipZ"];
                        }
                        newMap.Accepted = oldMap.accepted;
                        newMap.AcceptedById = tester.Id;

                        newMap.AtlasID = oldMap.id;

                        User author = UserMan[oldMap.author];
                        if (author != null) {
                            newMap.AuthorId = author.Id;
                        }

                        newMap.Filepath = "/" + cmrInt + "/" + oldMap.name;
                        newMap.IsDenied = false;
                        TimeSpan yearAgo = new TimeSpan(365, 0, 0, 0, 0);
                        newMap.LastModified = DB.UnixTimestamp;
                        newMap.Name = oldMap.name;

                        MapMan.AddMap(newMap);
                    }
                } catch (System.IO.FileNotFoundException e) {
                    Console.WriteLine("No file: " + filename + " found. Exception caught.");
                }
            }
            #endregion
            MapMan.DumpStateToFile(@"C:\CMR\MongoMapDumpAfterMigrateAdd.txt");

            Console.WriteLine("Done initializing database from deprecated filesystem structure. Press enter to continue.");
            Console.ReadLine();
        }

    }
}
