/**
 * FurkieDB.cs
 * File containing the Database connection junk to connect to a MongoDB.
 * @author Travis Drake
 */
#define DB_DEBUG  // if defined, will use debug database instead of production.
#define DB_CLONE_PRODUCTION_TO_DEBUG // if defined, the debug database will be cloned from the production db.
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
using UserCMR;
using MapCMR;


namespace DatabaseCMR {
	public class DBObject {
        [BsonId]
        public ObjectId Id;
    }

	public static class DB {
        private const string _CONNECTION_STRING = "mongodb://localhost";
		private const string _PRODUCTION_DB_NAME_DO_NOT_USE = "CmrDB";

				//FEEL FREE TO CHANGE THESE, ALL COLLECTIONS AND DATABASES WILL BE CREATED
#if DB_DEBUG	//WHEN THEY ARE FIRST USED, SO YOU DONT NEED TO "INITIALIZE" A NEW DB OR ANYTHING
		private const string _DB_NAME = "DebugDB";
#else
		private const string _DB_NAME = "CmrDB";
#endif
        public const string _USER_TABLE_NAME = "Users";
        public const string _IGN_TABLE_NAME = "Igns";
        public const string _MAP_TABLE_NAME = "Maps";
        public const string _DENIAL_TABLE_NAME = "Denials";

        private static MongoDatabase db;

        private static FurkieBot fb = FurkieBot.Instance;

        private static object _instanceLock = new Object();

        public static int UnixTimestamp { get { return (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; } }





        public static MongoDatabase Database {
            get {
                lock (_instanceLock) {
                    if (db == null) {
                        MongoClient client = new MongoClient(_CONNECTION_STRING);
                        MongoServer server = client.GetServer();
#if DB_CLONE_PRODUCTION_TO_DEBUG  &&  DB_DEBUG
						if (_PRODUCTION_DB_NAME_DO_NOT_USE != _DB_NAME) {
							//copy production database into debug database.
							MongoDatabase prod = server.GetDatabase(_PRODUCTION_DB_NAME_DO_NOT_USE);
							server.DropDatabase(_DB_NAME);
							db = server.GetDatabase(_DB_NAME);
							foreach (string collectionName in prod.GetCollectionNames()) {
								MongoCollection<BsonDocument> col = prod.GetCollection(collectionName);
								MongoCollection<BsonDocument> debug_col = db.GetCollection(collectionName);
								foreach (BsonDocument obj in col.AsQueryable<BsonDocument>()) {
									debug_col.Save(obj);
									Console.WriteLine("Copied over: " + obj.ToString());
								}
							}
						} else {
							Console.WriteLine("Could not copy production database to test database because they are the same DB name.");
						}
#endif
                        db = server.GetDatabase(_DB_NAME);
                    }
                }
                return db;
            }
        }
    }
}