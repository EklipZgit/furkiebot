using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Linq;
//may not need ^


namespace CmrMapManager {

    /// <summary>
    /// Denial objects to represent specific instances of denials
    /// </summary>
    class Denial {
        public string message;
        public string tester;
        public Denial(string theTester, string theMessage) {
            message = theMessage;
            tester = theTester;
        }

        public Denial()
            : this("Not Initialized", "Not Initialized") {

        }
    }

    /// <summary>
    /// Class containing all the data about a map.
    /// </summary>
    class CmrMap {
        private string name;
        private int id;
        private string filepath;
        private string author;
        private string acceptedBy;
        private bool accepted;
        private string modifiedTimestamp;
        private bool forceid;
        private List<Denial> denialMessages;

        public string Name { get { return name; } set { name = value; } }
        public int ID {
            get { return id; }
            set {
                if (!forceid) {
                    id = value;
                }
            }
        }
        public string Filepath { get { return filepath; } set { filepath = value; } }
        public string Author { get { return author; } set { author = value; } }
        public string AcceptedBy { get { return acceptedBy; } }
        public string LastModified { get { return modifiedTimestamp; } set { modifiedTimestamp = value; } }
        public bool IsForcedID { get { return forceid; } }
        public string DenialMessage {
            get { 
                return denialMessages[denialMessages.Count - 1].message; 
            } 
        }


        public CmrMap() {
        }


        public bool Accept(string tester, bool isAdmin=false) {
            if (accepted) {
                return false;
            } else {
                acceptedBy = tester;
                accepted = true;
                return true;
            }
        }

        public bool ForceID(int newId, string tester) {
            id = newId;
            forceid = true;
            return true;
        }

        public bool Deny(string tester, string reasonForDenial) {
            denialMessages.Add(new Denial(tester, reasonForDenial));
            return true;
        }
    }
    
        
    class MapManager {
        private static string CONNECTION_STRING = "mongodb://localhost";
        private static MongoClient CLIENT = new MongoClient(CONNECTION_STRING);
        static MongoServer SERVER = CLIENT.GetServer();
        MongoDatabase DB = SERVER.GetDatabase("CmrDB"); // "CmrDB" is the name of the database
        
        BsonClassMap.RegisterClassMap<MyClass>(cm => {
            cm.MapIdProperty(c => c.SomeProperty);
            // mappings for other fields and properties
        });
        
    }
}