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
using UserManager;
using MapManager;


namespace FurkieDB {
    class DBObject {
        [BsonId]
        public ObjectId Id;
    }
        
    static class DB {
        private const string _CONNECTION_STRING = "mongodb://localhost";
        private const string _DB_NAME = "CmrDB";
        public const string _USER_TABLE_NAME = "Users";
        public const string _IGN_TABLE_NAME = "Igns";
        public const string _MAP_TABLE_NAME = "Maps";
        public const string _DENIAL_TABLE_NAME = "Denials";

        private static MongoDatabase db;

        private static FurkieBot fb = FurkieBot.Instance;

        public static MongoDatabase Database {
            get {
                if (db == null) {
                    MongoClient client = new MongoClient(_CONNECTION_STRING);
                    MongoServer server = client.GetServer();
                    db = server.GetDatabase(_DB_NAME); // "CmrDB" is the name of the database
                } 
                return db;
            }
        }

        private DB() {
        }
    }
}