﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Permissions;
using System.Security.Cryptography;
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
using MapCMR;


namespace UserCMR {
    /// <summary>
    /// Class containing all the data about a User.
    /// </summary>
    class User : DBObject {
        private string nameLower;
        private string name;

        public string NameLower { get { return nameLower; } set { nameLower = value.Trim().ToLower(); } }
        /// <summary>
        /// Gets or sets the name. If setting, automatically sets NameLower as well.
        /// </summary>
        public string Name { 
            get { return name; } 
            set { 
                name = value.Trim(); 
                nameLower = name.ToLower(); 
            } 
        }
        public string Streamurl { get; set; }
        public bool Tester { get; set; }
        public bool Trusted { get; set; }
        public bool Admin { get; set; }
        public bool Notify { get; set; }
        public int Rating { get; set; }
        public int RandmapRating { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }




        [BsonConstructor]
        public User() {
            Rating = 0;
            RandmapRating = 0;
            Password = "";
            Salt = "";
            Streamurl = "";
            Name = "";
        }
    }


    /// <summary>
    /// An In-Game-Name class to encompass the values stored in the database.
    /// </summary>
    class IGN : DBObject {
        public string DustforceName;
        public ObjectId IrcUserID;

        public IGN(ObjectId id, string dustforceName, ObjectId ircUserId) {
            this.Id = id;
            this.DustforceName = dustforceName;
            this.IrcUserID = ircUserId;
        }

        [BsonConstructor]
        public IGN() {}
    }


    /// <summary>
    /// A Singleton class to manage Users in the Database.
    /// </summary>
    class UserManager {
        private MongoCollection<User> Users = DB.Database.GetCollection<User>(DB._USER_TABLE_NAME);
        private MongoCollection<IGN> Igns = DB.Database.GetCollection<IGN>(DB._IGN_TABLE_NAME);
        private static UserManager instance;
        private static object _instanceLock = new Object();
        public static UserManager Instance {
            get {
                lock (_instanceLock) {
                    if (instance == null) {
                        instance = new UserManager();
                    }
                }
                return instance;
            }
        }
            
        private FurkieBot fb = FurkieBot.Instance;

        private UserManager() {
            if (!BsonClassMap.IsClassMapRegistered(typeof(User))) {
                BsonClassMap.RegisterClassMap<User>(cm => {
                    //for private stuff, have constructor with below form
                    cm.AutoMap();
                });
            }
            if (!BsonClassMap.IsClassMapRegistered(typeof(IGN))) {
                BsonClassMap.RegisterClassMap<IGN>(cm => {
                    cm.AutoMap();
                });
            }
        }


        /// <summary>
        /// Gets the <see cref="User"/> with the specified identifier from the Users Mongo Collection
        /// </summary>
        /// <param name="id">The identifier to look for.</param>
        /// <returns>The user with that ID.</returns>
        public User this[ObjectId id] {
            get {
                return Users.AsQueryable<User>()
                    .Where<User>(u => u.Id == id)
                    .First<User>();
            }
        }


        /// <summary>
        /// Gets the <see cref="User"/> with the specified name out of the Users mongocollection.
        /// </summary>
        /// <param name="name">The name of the user to find.</param>
        /// <returns>The user by that name.</returns>
        public User this[string name] {
            get {
                return Users.AsQueryable()
                    .Where(u => u.NameLower == name.ToLower().Trim())
                    .First();
            }
        }


        /// <summary>
        /// Saves the user to the Users table.
        /// </summary>
        /// <param name="user">The user.</param>
        public void SaveUser(User user) {
            var result = Users.Save(user);
        }



        /// <summary>
        /// Saves the ign to the Igns table.
        /// </summary>
        /// <param name="user">The ign.</param>
        public void SaveIgn(IGN ign) {
            var result = Igns.Save(ign);
        }



        /// <summary>
        /// Gets the users in a Queryable format.
        /// </summary>
        /// <returns></returns>
        public IQueryable<User> GetUsers() {
            return Users.AsQueryable<User>();
        }


        /// <summary>
        /// Gets the igns in a Queryable format.
        /// </summary>
        /// <returns></returns>
        public IQueryable<IGN> GetIgns() {
            return Igns.AsQueryable<IGN>();
        }





        /// <summary>
        /// Determines whether the specified nick is registered.
        /// </summary>
        /// <param name="nick">The nickname.</param>
        /// <returns>bool whether or not the nick is registered.</returns>
        public bool IsRegistered(string nick, string toNotify = null) {
            User user = this[nick];
            bool registered = false;
            if (user != null) {
                if (user.Password != "" && user.Password != null) {
                    registered = true;
                } else if (toNotify != null) {
                    fb.Msg(toNotify, "User \"" + nick.ToLower() + "\" has not yet registered with FurkieBot.");
                }
            } else if (toNotify != null) {
                fb.Msg(toNotify, "No user \"" + nick.ToLower() + "\" in Users database.");
            }
            return registered;
        }



        /// <summary>
        /// Determines whether the specified nick is an admin.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="toNotify">The IRC user initiating this check.</param>
        /// <returns>bool whether or not the nick is an admin.</returns>
        public bool IsAdmin(string nick, string toNotify = null) {
            bool isAdmin = false;
            if (IsRegistered(nick, toNotify) && fb.IsIdentified(nick, toNotify)) {
                isAdmin = this[nick].Admin;
                if (!isAdmin && toNotify != null) {
                    fb.Msg(toNotify, "User \"" + nick.ToLower() + "\" is not an Admin.");
                }
            } 
            return isAdmin;
        }




        /// <summary>
        /// Determines whether the specified nick is a tester.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="toNotify">To notify.</param>
        /// <returns></returns>
        public bool IsTester(string nick, string toNotify = null) {
            if (fb.IsIdentified(nick, toNotify)) {
                if (IsRegistered(nick, toNotify)) {
                    return this[nick].Tester;
                } 
            } 
            return false;
        }



        /// <summary>
        /// Determines whether the specified nick is trusted.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="toNotify">To notify.</param>
        /// <returns></returns>
        public bool IsTrusted(string nick, string toNotify = null) {
            if (fb.IsIdentified(nick, toNotify)) {
                if (IsRegistered(nick, toNotify)) {
                    return this[nick].Trusted;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }




        /// <summary>
        /// Attempts to set the Users password to the given password. 
        /// If no user exists by this nickname, create a new user.
        /// </summary>
        /// <param name="nickname">The nickname.</param>
        /// <param name="password">The password.</param>
        public void AttemptRegistration(string nickname, string password) {
            User user = this[nickname];
            bool wasNull = false;
            if (user == null) {
                user = new User();
                user.Name = nickname;
                wasNull = true;
            }
            string nickLower = user.NameLower;
            if (fb.IsIdentified(nickLower, nickname)) {
                string[] hashes = GeneratePasswordHashes(password.Trim());
                if (!wasNull) {
                    
                    user.Salt = hashes[0];
                    user.Password = hashes[1];
                    user.Name = nickname;
                    //userlist.Remove(nickname);
                    //userlist.Add(nickname, info);
                    SaveUser(user);
                } else {
                    user.Salt = hashes[0];
                    user.Password = hashes[1];
                    user.Name = nickname;
                    SaveUser(user);
                }
                fb.Notice(nickname, "Successfully registered your nick with FurkieBot! Dont forget your password. You can always re-register if you forget the password.");
                fb.Notice(nickname, "You will now want to set your in-game dustforce name with FurkieBot. use \".setign <steam / drm-free name>\" to set your IGN with FurkieBot.");
            } else {
                fb.NoticeNotIdentified(nickname);
            }
        }








        /// <summary>
        /// Resets all users' tester status to false.
        /// </summary>
        public void ResetTesters() {
            //Dictionary<string, PlayerInfo> newUserList = new Dictionary<string, PlayerInfo>(userlist.Count * 2);
            var testers = GetTesters().ToList<User>();
            foreach (User user in testers) {
                user.Tester = false;
                fb.Notice(user.Name, "Your tester status has been reset.");
                SaveUser(user);
            }
            fb.MsgChans("All testers have been reset to non testers. If you want to be a tester for the next CMR use \".settester true\" provided you have tester permissions. If you try this and don't have permissions, ask an admin.");
        }



        /// <summary>
        /// Gets a Queryable list of testers.
        /// </summary>
        /// <returns></returns>
        public IQueryable<User> GetTesters() {
            return Users.AsQueryable<User>()
                .Where(c => c.Tester == true)
                .OrderBy(c => c.Name);
        }



        /// <summary>
        /// Gets a Queryable list of testers.
        /// </summary>
        /// <returns></returns>
        public IQueryable<User> GetTrustedUsers() {
            return Users.AsQueryable<User>()
                .Where(c => c.Trusted == true)
                .OrderBy(c => c.Name);
        }



        /// <summary>
        /// Gets a Queryable list of testers.
        /// </summary>
        /// <returns></returns>
        public IQueryable<User> GetAdmins() {
            return Users.AsQueryable<User>()
                .Where(c => c.Admin == true)
                .OrderBy(c => c.Name);
        }





        /// <summary>
        /// Gets the user ircname by the provided dustforceuser name.
        /// </summary>
        /// <param name="dustforceuser">The dustforceuser.</param>
        /// <returns>null if not found</returns>
        public User GetUserByIGN(string ign) {
            ObjectId id = Igns.AsQueryable<User>()
                .Where(c => c.NameLower == ign.ToLower())
                .Select<User, ObjectId>(c => c.Id)
                .First();
            if (id != null) {
                return this[id];
            } else {
                Console.WriteLine("Got a null id after querying Igns...");
                return null;
            }
        }


        /// <summary>
        /// Gets the ingame name object for the specified name.
        /// </summary>
        /// <param name="dustforcename">The dustforcename.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Found multiple entries in the Igns table matching:  + dustforcename</exception>
        public IGN GetIgnByDustforcename(string dustforcename) {
            var query = Query.EQ("NameLower", dustforcename.ToLower().Trim());
            var results = Igns.Find(query);
            if (results.Count() > 1) {
                throw new Exception("Found multiple entries in the Igns table matching: " + dustforcename);
            }
            return results.First();
        }



        /// <summary>
        /// Gets the users in game dustforce name.
        /// </summary>
        /// <param name="ircuser">The nick of the user.</param>
        /// <returns>The users IGN.</returns>
        public IGN GetIgnByIrc(string ircuser) {
            User user = this[ircuser];
            if (user != null) {
                var query = Query.EQ("IrcUserID", user.Id);
                var matches = Igns.Find(query);
                if (matches.Count() > 1) {
                    throw new Exception("in GetUserIgn, multiple matches in Igns for user.Id: " + user.Id);
                }
                return matches.First<IGN>();
            } else {
                return null;
            }
        }




        /// <summary>
        /// Sets a users in game dustforce name.
        /// </summary>
        /// <param name="ircuser">The ircuser whose name to set.</param>
        /// <param name="dustforcename">The users dustforce name.</param>
        public bool SetIgnByIrc(string ircuser, string dustforcename) {
            User user = this[ircuser];
            IGN ign = GetIgnByDustforcename(dustforcename);
            if (ign != null) {
                User ignUser = GetUserByIGN(ign.DustforceName);
                //if (!|| (dustforcelist[dustforceuser].ircname.ToLower() == ircLower)) {
                if (ignUser != null && ignUser.Id != user.Id) {
                    fb.Notice(ircuser, "That IGN is already registered to someone else. Perhaps you registered it under another IRC nickname? If this is an issue, ask an admin to use .deleteign on that IGN.");
                    return false;
                } else { //ignUser was null, or it matches the expected userId. So update.
                    string oldname = ign.DustforceName;

                    ign.DustforceName = dustforcename;

                    SaveIgn(ign);
                    return true;
                }
            } else {
                ign = new IGN();
                ign.DustforceName = dustforcename;
                ign.IrcUserID = user.Id;
                SaveIgn(ign);
                return true;
            }
        }



        /// <summary>
        /// Gets the users rating.
        /// </summary>
        /// <param name="ircuser">The ircuser.</param>
        /// <returns>The users rating.</returns>
        public int GetUserRating(string ircuser) {
            User user = this[ircuser];
            if (user != null) {
                return user.Rating;
            } else {
                return -1;
            }
        }


        /// <summary>
        /// Removes the provided IGN from any nicks that use it.
        /// </summary>
        /// <param name="ign">The ign.</param>
        public bool RemoveIGN(string dustforcename, string toNotify) {
            IGN ign = GetIgnByDustforcename(dustforcename);
            var query = Query.EQ("DustforceName", dustforcename);
            var result = Igns.Remove(query);
            return result.DocumentsAffected > 0;
        }


        /// <summary>
        /// Sets a user irc notify on/off
        /// </summary>
        /// <param name="ircuser">The ircuser whose name to set.</param>
        /// <param name="option">On or Off</param>
        public void SetUserNotify(string ircuser, bool option) {
            User user = this[ircuser];

            user.Notify = option;
            SaveUser(user);
        }






        private static string[] GeneratePasswordHashes(string pwTextString) {
            // If salt is not specified, generate it on the fly.
            // Define min and max salt sizes.
            int minSaltSize = 4;
            int maxSaltSize = 8;
            // Generate a random number for the size of the salt.
            Random random = new Random();
            int saltSize = random.Next(minSaltSize, maxSaltSize);

            // Allocate a byte array, which will hold the salt.
            byte[] saltBytes = new byte[saltSize];

            // Initialize a random number generator.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

            // Fill the salt with cryptographically strong byte values.
            rng.GetNonZeroBytes(saltBytes);


            // Because we support multiple hashing algorithms, we must define
            // hash object as a common (abstract) base class. We will specify the
            // actual hashing algorithm class later during object creation.
            HashAlgorithm hash = new SHA256Managed();


            byte[] saltHashBytes = hash.ComputeHash(saltBytes);
            string saltHashString = Convert.ToBase64String(saltHashBytes).Substring(0, 4);




            string finalHashString = HashSaltPw(hash, pwTextString, saltHashString);

            // Copy hash bytes into resulting array.
            string[] returnArray = { saltHashString, finalHashString };


            if (!VerifyHash(pwTextString, saltHashString, finalHashString)) {
                throw new Exception("wow ok fuck you");
            }


            return returnArray;
        }





        /// <summary>
        /// Returns the Hash of the password hash concatenated with the salt hash.
        /// </summary>
        /// <param name="hash">The hash algorithm.</param>
        /// <param name="pwTextString">The pw text string.</param>
        /// <param name="saltHashString">The salt hash string.</param>
        /// <returns>The hack of the password concatenated with the salt hash.</returns>
        private static string HashSaltPw(HashAlgorithm hash, string pwTextString, string saltHashString) {
            // Convert plain text into a byte array.
            byte[] pwTextBytes = Encoding.UTF8.GetBytes(pwTextString.Trim());


            // Compute hash value of our plain text with appended salt.
            byte[] pwTextHashBytes = hash.ComputeHash(pwTextBytes);
            string pwHash = Convert.ToBase64String(pwTextHashBytes);

            string saltAndPwHash = saltHashString + pwHash;
            byte[] saltPwHashBytes = Encoding.UTF8.GetBytes(saltAndPwHash);

            byte[] finalHashBytes = hash.ComputeHash(saltPwHashBytes);
            string finalHashString = Convert.ToBase64String(finalHashBytes);
            return finalHashString;
        }




        /**
         * <summary>
         * Compares a hash of the specified plain text value to a given hash
         * value. Plain text is hashed with the same salt value as the original
         * hash.
         * </summary>
         * <param name="pwTextString">
         * Plain text to be verified against the specified hash. The function
         * does not check whether this parameter is null.
         * </param>
         * <param name="salt">
         * The salt used to encrypt the password.
         * </param>
         * <param name="expectedHashString">
         * Base64-encoded hash value produced by ComputeHash function. This value
         * includes the original salt appended to it.
         * </param>
         * <returns>
         * If computed hash mathes the specified hash the function the return
         * value is true; otherwise, the function returns false.
         * </returns>
         */
        private static bool VerifyHash(string pwTextString, string salt, string expectedHashString) {
            // Convert base64-encoded hash value into a byte array.

            HashAlgorithm hash = new SHA256Managed();


            // Convert plain text into a byte array.
            byte[] pwTextBytes = Encoding.UTF8.GetBytes(pwTextString.Trim());


            // Compute hash value of our plain text with appended salt.
            byte[] pwTextHashBytes = hash.ComputeHash(pwTextBytes);
            string pwHash = Convert.ToBase64String(pwTextHashBytes);

            string saltAndPwHash = salt + pwHash;
            byte[] saltPwHashBytes = Encoding.UTF8.GetBytes(saltAndPwHash);

            byte[] finalHashBytes = hash.ComputeHash(saltPwHashBytes);
            string finalHashString = Convert.ToBase64String(finalHashBytes);

            return (expectedHashString == finalHashString);
        }

    }
}