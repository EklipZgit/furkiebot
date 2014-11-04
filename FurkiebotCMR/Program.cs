﻿/**
 * FurkieBot
 * Program.cs
 * @author Furkan Pham (Furkiepurkie)
 * @author Travis Drake (EklipZ)
 */


/*
 * IRC CODES https://www.alien.net.au/irc/irc2numerics.html
 */

//#define FB_DEBUG  //If defined furkiebot will use debug channels and shit.

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Data;
using System.Data.SqlClient;
using System.Security.Permissions;
using System.Security.Cryptography;
using ClosedXML.Excel;
using System.Net;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Xml;
using TraxBusterCMR;
using AtlasTools;

/// <summary>
/// The Furkiebot namespace.
/// </summary>
namespace FurkiebotCMR {
    /// <summary>
    /// The irc configuration parameters that get passed into FurkieBot.
    /// </summary>
    public struct IRCConfig {
        public string server;
        public int port;
        public string nick;
        public string name;
        public string pass;
        public string altNick;
    } /* IRCConfig */



    /// <summary>
    /// Player info struct containing all information about a player.
    /// </summary>
    public struct PlayerInfo {
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
    /// A FurkieBot IRC bot.
    /// </summary>
    public class FurkieBot : IDisposable {

#if FB_DEBUG
        public const string BOT_NAME = "FurkieBot_";
        public const string MAIN_CHAN = "#dustforcee";
        public const string CMR_CHAN = "#DFcmrr";
#else
        public const string BOT_NAME = "FurkieBot";
        public const string MAIN_CHAN = "#dustforce";
        public const string CMR_CHAN = "#DFcmr";
#endif


        #region IRC VALUES
        public static string SEP = ColourChanger(" | ", "07"); //The orange | seperator also used by GLaDOS
        public const char ACT = (char)1;
        public const string MOLLY = "༼ つ ◕_◕ ༽つ MOLLY ༼ つ ◕_◕ ༽つ";
        #endregion
        #region PATHS
        public const string MAPS_PATH = @"C:\CMR\Maps\";
        public const string DATA_PATH = @"C:\CMR\Data\";
        public const string PASS_PATH = DATA_PATH + "Password.txt";
        public const string USERLIST_PATH = DATA_PATH + @"Userlist\userlistmap.json";
        #endregion
        #region LINKS
        public const string URL_BASE = @"http://eklipz.us.to/cmr/";
        public const string DOWNLOAD_LINK = URL_BASE + @"downloadmap.php?map=";
        public const string TEST_LINK = URL_BASE + @"maptest.php";
        public const string UPLOAD_LINK = URL_BASE + @"map.php";
        public const string WIKI_LINK = @"https://github.com/EklipZgit/furkiebot/wiki";
        public const string MAP_PACK_LINK = @"http://redd.it/279zmi";
        public const string INFO_LINK = URL_BASE;
        #endregion


        #region BOT DEFAULTS
        public const int MAX_MSG_LENGTH = 450;
        #endregion

        public const int MIN_MAPS = 7;

        private TraxBuster buster;
        private Thread busterThread;

        private AtlasChecker checker;

        private int acceptedCount;
        private int pendingCount;


        /// <summary>
        /// The last slapper
        /// </summary>
        private string lastSlapper = "";
        /// <summary>
        /// The number of times the last slap requester has requested a slap in a row.
        /// </summary>
        private int repeatSlaps = 0;

        /// <summary>
        /// The last person to request a pasta
        /// </summary>
        private string lastPastaer = "";
        /// <summary>
        /// The number of times the last pasta requester has requested a pasta in a row.
        /// </summary>
        private int repeatPastas = 0;


        /// <summary>
        /// How often for furkiebot to harass people for talking about him like he's not there.
        /// </summary>
        private const int MENTION_EVERY = 12;
        /// <summary>
        /// The number of times furkiebots name has been mentioned in chat.
        /// </summary>
        private int furkiebotMentionCount = 0;



        /// <summary>
        /// A flag that is set when FurkieBot itself modified the map file, so that the change handler knows not to reload anything.
        /// </summary>
        private bool ignoreChangedMaps = false;

        /// <summary>
        /// Notifies FurkieBot to reload the map data. This avoid Threading issues by ensuring critical data is only modified by the core FurkieBot thread.
        /// </summary>
        private bool notifyReloadMaps = false;


        /// <summary>
        /// A flag that is set when FurkieBot itself modified the userlist file, so that the change handler knows not to reload anything.
        /// </summary>
        private bool ignoreChangedUserlist = false;


        /// <summary>
        /// Notifies FurkieBot to reload the userlist data. This avoid Threading issues by ensuring critical data is only modified by the core FurkieBot thread.
        /// </summary>
        private bool notifyReloadUserlist = false;



        /// <summary>
        /// Locks multithreading down for the Whois checking sections, so that no threads can be waiting on whois's at once.
        /// </summary>
        private readonly object _whoisLock = new Object();


        /// <summary>
        /// Locks multithreading down for the map update checking sections, so that the maplist cannot be used at the same time it is being updated.
        /// Public so that outside sources who alter maps and then set externally can lock this.
        /// </summary>
        public readonly object _updatingMapsLock = new Object();


        /// <summary>
        /// Locks multithreading down for the userlist update checking sections, so that the userlist cannot be used at the same time it is being updated.
        /// </summary>
        private readonly object _updatingUserlistLock = new Object();


        private readonly object _sendingMessageLock = new Object();



        private TcpClient IRCConnection = null;
        private IRCConfig config;
        private NetworkStream ns = null;
        private BufferedStream bs = null;
        private StreamReader sr = null;
        private StreamWriter sw = null;

        //watches the maps.json file
        private FileSystemWatcher mapsWatcher;
        //watches the userlist.json file
        private FileSystemWatcher userlistWatcher;


        private DataTable racers;
        //private DataTable users;

        private Dictionary<string, MapData> maps;


        /// <summary>
        /// Allows FurkieBot to instantly respond to changes in the map file without actually modifying maps while anything is running.
        /// </summary>
        private Dictionary<string, MapData> mapsTemp;

        private Dictionary<string, PlayerInfo> userlist; //ircnames -> userinfo. used for quick lookup and serializing to userlistmap.json upon modification
        private Dictionary<string, PlayerInfo> userlistTemp; //temp file to store the userlist temporarily until update.
        private Dictionary<string, PlayerInfo> dustforcelist;// dustforcenames -> userinfo. used only for quick lookup, and duplicate dustforcename checking.
        private Dictionary<string, bool> identlist;// dustforcenames -> userinfo. used only for quick lookup, and duplicate dustforcename checking.


        public string dummyRacingChan; //first part of racingchannel string
        public string realRacingChan; //real racing channel string
        public string mainchannel; //also the channel that will be joined upon start, change to #dustforcee for testing purposes
        public string cmrchannel; //also the channel that will be joined upon start, change to #dustforcee for testing purposes
        public int cmrId;
        private string comNames; // Used for NAMES commands
        private bool complexAllowed; //Set to false when a function is already waiting on a complex return, ie IsIdentified while parsing a /whois. Keeps additional complex functions from starting in the meantime.

        private TimeSpan cmrtime;//At what time (local) it is possible to start a CMR, 8:30pm equals 6:30pm GMT for me  EDIT now 10:30 AM PST for 6:30 GMT
        private string cmrtimeString; //make sure this equals the time on TimeSpan cmrtime

        private string cmrStatus;

        bool hype; //Just for .unhype command lol

        Stopwatch stahpwatch; //Timer used for races
        Stopwatch countdown; //Timer used to countdown a race




        /// <summary>
        /// Initializes a new instance of the <see cref="FurkieBot"/> class.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public FurkieBot(IRCConfig config) {
            this.config = config;   // Create a new FileSystemWatcher and set its properties.



            //DataTable of Racers for the current CMR
            racers = new DataTable();
            racers.Columns.Add("Name", typeof(string));
            racers.Columns.Add("Status", typeof(int)); //1 = done; 2 = racing; 3 = ready; 4 = forfeit; 5 = dq; 6 = standby
            racers.Columns.Add("Hour", typeof(int));
            racers.Columns.Add("Min", typeof(int));
            racers.Columns.Add("Sec", typeof(int));
            racers.Columns.Add("TSec", typeof(int));
            racers.Columns.Add("Comment", typeof(string));
            racers.Columns.Add("Rating", typeof(int)); //Currently not being used, may or may not be used in the future, bird knows what's up


            cmrId = GetCurrentCMRidFromFile();

            maps = DeserializeMaps(cmrId);

            loadUserlist();

            dummyRacingChan = "#cmr-"; //first part of racingchannel string
            realRacingChan = ""; //real racing channel string
            mainchannel = MAIN_CHAN; //also the channel that will be joined upon start, change to #dustforcee for testing purposes
            cmrchannel = CMR_CHAN;
            comNames = ""; // Used for NAMES commands

            complexAllowed = true;


            cmrStatus = GetCurrentCMRStatus(); //CMR status can be closed, open, racing or finished
            identlist = new Dictionary<string, bool>();

            hype = true; //Just for .unhype command lol

            stahpwatch = new Stopwatch(); //Timer used for races
            countdown = new Stopwatch(); //Timer used to countdown a race


            cmrtime = new TimeSpan(10, 30, 0); //At what time (local) it is possible to start a CMR, 8:30pm equals 6:30pm GMT for me  EDIT now 11:30 AM PST for 6:30 GMT
            cmrtimeString = @"11:30:00"; //make sure this equals the time on TimeSpan cmrtime




            /*
             * Set up the event handlers for watching the CMR map filesystem. Solution for now. 
             */
            //pendingWatcher = new FileSystemWatcher();
            //acceptedWatcher = new FileSystemWatcher();
            mapsWatcher = new FileSystemWatcher();
            userlistWatcher = new FileSystemWatcher();
            InitDirectories();
            mapsWatcher.Path = MAPS_PATH + cmrId;
            userlistWatcher.Path = DATA_PATH + "\\Userlist";
            /* Watch for changes in LastWrite times, and
               the renaming of files or directories. */
            //pendingWatcher.NotifyFilter = NotifyFilters.LastWrite
            //   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            //acceptedWatcher.NotifyFilter = NotifyFilters.LastWrite
            //   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            mapsWatcher.NotifyFilter = NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            userlistWatcher.NotifyFilter = NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            //fileWatcher.Filter = "*.txt";

            // Add event handlers.
            ////pendingWatcher.Changed += new FileSystemEventHandler(OnChanged);
            //pendingWatcher.Created += new FileSystemEventHandler(CreatedPending);
            //pendingWatcher.Deleted += new FileSystemEventHandler(DeletedPending);
            ////pendingWatcher.Renamed += new RenamedEventHandler(OnRenamed);

            ////acceptedWatcher.Changed += new FileSystemEventHandler(OnChanged);
            //acceptedWatcher.Created += new FileSystemEventHandler(CreatedAccepted);
            //acceptedWatcher.Deleted += new FileSystemEventHandler(DeletedAccepted);
            //acceptedWatcher.Renamed += new RenamedEventHandler(OnRenamed);

            mapsWatcher.Changed += new FileSystemEventHandler(ChangedMaps);
            userlistWatcher.Changed += new FileSystemEventHandler(ChangedUserlist);
            //mapsWatcher.Created += new FileSystemEventHandler(CreatedMaps);
            //mapsWatcher.Renamed += new RenamedEventHandler(OnRenamed);



            //// Begin watching.
            mapsWatcher.EnableRaisingEvents = true;
            userlistWatcher.EnableRaisingEvents = true;
        }




        /// <summary>
        /// Messages the racechan. Intended to be called by outside classes and threads to cause FurkieBot to notify the race channel about specific things.
        /// </summary>
        /// <param name="message">The message for the race channel</param>
        public void MessageRacechan(string message) {
            sendData("PRIVMSG", " " + realRacingChan + " :" + message);
        }




        public Dictionary<string, MapData> GetMaps() {
            if (this.mapsTemp != null) {
                return new Dictionary<string, MapData>(this.mapsTemp);
            } else {
                return new Dictionary<string, MapData>(this.maps);
            }
        }


        public void SetMaps(Dictionary<string, MapData> maps) {
            lock (_updatingMapsLock) {
                if (this.mapsTemp != null) {
                    this.mapsTemp = maps;
                } else {
                    this.maps = maps;
                }
                WriteMaps(maps, cmrId);
            }
        }



        public Dictionary<string, PlayerInfo> GetUsers() {
            return new Dictionary<string, PlayerInfo>(this.userlist);
        }


        public void SetUsers(Dictionary<string, PlayerInfo> users) {
            lock (_updatingUserlistLock) {
                this.userlist = users;
                WriteUsers();
            }
        }


        public int PendingCount {
            get {
                return pendingCount;
            }
        }


        public int AcceptedCount {
            get {
                return acceptedCount;
            }
        }



        /// <summary>
        /// Initializes the File watching handlers and directories.
        /// </summary>
        private void InitDirectories() {
            Directory.CreateDirectory(MAPS_PATH + cmrId);
        }


        // Define the filesystem event handlers. 

        /// <summary>
        /// The EventHandler function for when the maps json file is changed.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RenamedEventArgs"/> instance containing the event data.</param>
        private void ChangedUserlist(object source, FileSystemEventArgs e) {
            if (e.Name == "userlistmap.json") {
                Console.WriteLine("Userlist file changed.");
                if (!ignoreChangedUserlist) {
                    notifyReloadUserlist = true;
                    SyncUserList();
                } else {
                    ignoreChangedUserlist = false;
                }
            }
        }


        // Define the filesystem event handlers. 

        /// <summary>
        /// The EventHandler function for when the maps json file is changed.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RenamedEventArgs"/> instance containing the event data.</param>
        private void ChangedMaps(object source, FileSystemEventArgs e) {
            if (e.Name == "maps.json") {
                Console.WriteLine("Maps file changed.");
                if (!ignoreChangedMaps) {
                    notifyReloadMaps = true;
                    SyncMapList();
                } else {
                    ignoreChangedMaps = false;
                }
            }
        }



        /// <summary>
        /// The EventHander function for when the maps json file is created.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="e">The <see cref="FileSystemEventArgs"/> instance containing the event data.</param>
        private static void CreatedMaps(object source, FileSystemEventArgs e) {
            throw new Exception("this shit should never get called???????");
        }

        /// <summary>
        /// Outputs the current CMR's map status to the provided channel.
        /// </summary>
        /// <param name="chan">The IRC channel to output to. If empty or null, output to both main channels.</param>
        private void OutputMapStatus(string chan) {
            OutputPending(chan);
            OutputAccepted(chan);
        }




        /// <summary>
        /// Outputs the current CMR pending map status to the provided channel.
        /// </summary>
        /// <param name="chan">The channel. If empty or null, output to both main channels.</param>
        private void OutputPending(string chan) {
            string toSay = " :";
            string mapString = "";
            int pendingcount = 0;
            foreach (KeyValuePair<string, MapData> entry in maps) {
                if (entry.Value.accepted == false) {
                    mapString += "\"" + entry.Value.name + "\" by " + entry.Value.author + SEP;
                    pendingcount++;
                    //toSay += "\" by " + entry.Value.author;       // TODO comment back in if we want authors in maplist.
                }
            }
            if (pendingcount > 0) {
                mapString = mapString.Substring(0, mapString.Length - SEP.Length);
            }

            toSay += pendingcount + " maps pending: " + mapString;
            if (chan == null || chan == "" || chan == " ") {
                sendData("PRIVMSG", mainchannel + toSay);
                sendData("PRIVMSG", cmrchannel + toSay);
            } else {
                sendData("PRIVMSG", chan + toSay);
            }
        }




        /// <summary>
        /// Outputs the current CMR accepted map status to the provided channel.
        /// </summary>
        /// <param name="chan">The channel. If empty or null, output to both main channels.</param>
        private void OutputAccepted(string chan) {
            string toSay = " :";
            string mapString = "";
            int acceptedcount = 0;
            foreach (KeyValuePair<string, MapData> entry in maps) {
                if (entry.Value.accepted == true) {
                    mapString += "\"" + entry.Value.name + "\" by " + entry.Value.author + SEP;
                    acceptedcount++;
                    //toSay += "\" by " + entry.Value.author;       // TODO comment back in if we want authors in maplist.
                }
            }

            if (acceptedcount > 0) {
                mapString = mapString.Substring(0, mapString.Length - SEP.Length);
            }

            toSay += acceptedcount + " maps accepted: " + mapString;
            if (chan == null || chan == "" || chan == " ") {
                sendData("PRIVMSG", mainchannel + toSay);
                sendData("PRIVMSG", cmrchannel + toSay);
            } else {
                sendData("PRIVMSG", chan + toSay);
            }
        }


        private void OutputTesters(string chan) {
            string testerString = "";
            int acceptedcount = 0;
            foreach (KeyValuePair<string, PlayerInfo> entry in userlist) {
                if (entry.Value.tester == true) {
                    testerString += entry.Value.ircname + SEP;
                    acceptedcount++;
                    //toSay += "\" by " + entry.Value.author;       // TODO comment back in if we want authors in maplist.
                }
            }

            if (acceptedcount > 0) {
                testerString = testerString.Substring(0, testerString.Length - SEP.Length);
            }

            string toSay = "Map Testers for this CMR: " + testerString;
            if (chan == null || chan == "" || chan == " ") {
                Msg(mainchannel, toSay);
                Msg(cmrchannel, toSay);
            } else {
                Msg(chan, toSay);
            }
        }


        private void OutputTrusted(string chan) {
            string trustedString = "";
            int acceptedcount = 0;
            foreach (KeyValuePair<string, PlayerInfo> entry in userlist) {
                if (entry.Value.trusted == true) {
                    trustedString += entry.Value.ircname + SEP;
                    acceptedcount++;
                    //toSay += "\" by " + entry.Value.author;       // TODO comment back in if we want authors in maplist.
                }
            }

            if (acceptedcount > 0) {
                trustedString = trustedString.Substring(0, trustedString.Length - SEP.Length);
            }

            string toSay = "Trusted Users: " + trustedString;
            if (chan == null || chan == "" || chan == " ") {
                Msg(mainchannel, toSay);
                Msg(cmrchannel, toSay);
            } else {
                Msg(chan, toSay);
            }
        }


        private void OutputAdmins(string chan) {
            string adminString = "";
            int acceptedcount = 0;
            foreach (KeyValuePair<string, PlayerInfo> entry in userlist) {
                if (entry.Value.admin == true) {
                    adminString += entry.Value.ircname + SEP;
                    acceptedcount++;
                    //toSay += "\" by " + entry.Value.author;       // TODO comment back in if we want authors in maplist.
                }
            }

            if (acceptedcount > 0) {
                adminString = adminString.Substring(0, adminString.Length - SEP.Length);
            }

            string toSay = "Admins: " + adminString;
            if (chan == null || chan == "" || chan == " ") {
                Msg(mainchannel, toSay);
                Msg(cmrchannel, toSay);
            } else {
                Msg(chan, toSay);
            }
        }







        /// <summary>
        /// Loads the userlist from the userlist map file into the userlist Dictionary.
        /// </summary>
        private void loadUserlist() {
            userlist = getUserList();
            //Dictionary<string, PlayerInfo> temp = new Dictionary<string, PlayerInfo>();
            //foreach (KeyValuePair<string, PlayerInfo> entry in userlist) {
            //    PlayerInfo info = entry.Value;
            //    if (info.streamurl == "http://twitch.tv") {
            //        info.streamurl = "";
            //    }
            //    temp[entry.Key] = info;
            //}
            //userlist = temp;
            //WriteUsers();
            SyncOtherTables();
        }




        /// <summary>
        /// Gets the user list and returns it.
        /// </summary>
        /// <returns>The current userlist loaded from disk.</returns>
        private Dictionary<string, PlayerInfo> getUserList() {
            string filepath = USERLIST_PATH;

            while (true) {
                try {
                    string[] jsonarray = File.ReadAllLines(filepath);
                    string json = string.Join("", jsonarray);
                    return JsonConvert.DeserializeObject<Dictionary<string, PlayerInfo>>(json); // initially loads the userlist from JSON
                } catch (System.IO.IOException e) {
                    Console.WriteLine("Got an error trying to read the userlist. Error: ");
                    Console.WriteLine(e.StackTrace);
                }
                Thread.Sleep(5);
            }
        }



        /// <summary>
        /// Returns a the maplist for the specified CMRID deserialized from disk.
        /// </summary>
        /// <param name="cmrid">The CMR identifier.</param>
        /// <returns>The loaded map list. If the file doesnt exist, returns an empty maplist.</returns>
        private Dictionary<string, MapData> DeserializeMaps(int cmrid) {
            
            string mapsJsonPath = MAPS_PATH + cmrid + @"\maps.json";
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
                        return JsonConvert.DeserializeObject<Dictionary<string, MapData>>(jsonPending); // initially loads the userlist from JSON
                    } catch (System.IO.IOException e) {
                        Console.WriteLine("Got an error trying to read file. Error: ");
                        Console.WriteLine(e.StackTrace);
                    }
                    Thread.Sleep(5);
                }
            } else {
                return new Dictionary<string, MapData>();
            }


            // OLD
                //string acceptedFiles = MAPS_PATH + cmrId + @"\accepted\accepted.json"; // !! FILEPATH !!
                //string pendingFiles = MAPS_PATH + cmrId + @"\pending\pending.json"; // !! FILEPATH !!
                //string[] arraypending = File.ReadAllLines(pendingFiles);
                //string[] arrayaccepted = File.ReadAllLines(acceptedFiles);
                //string jsonPending = string.Join("", arraypending);
                //string jsonAccepted = string.Join("", arrayaccepted);

                //pendingMaps = JsonConvert.DeserializeObject<List<MapData>>(jsonPending); // initially loads the userlist from JSON
                //acceptedMaps = JsonConvert.DeserializeObject<List<MapData>>(jsonAccepted); // initially loads the userlist from JSON
            // END OLD
        }



        /// <summary>
        /// Syncronizes other dictionaries.
        /// </summary>
        private void SyncOtherTables() {
            dustforcelist = new Dictionary<string, PlayerInfo>();
            Dictionary<string, PlayerInfo> temp = (userlistTemp == null ? userlist : userlistTemp);
            foreach (KeyValuePair<string, PlayerInfo> entry in temp) {
                if (!dustforcelist.ContainsKey(entry.Value.dustforcename)) {
                    dustforcelist.Add(entry.Value.dustforcename, entry.Value);
                }
            }
        } /* IRCBot */





        /// <summary>
        /// Helper function to be called before handling new IRC input to make sure the bots state is up to date with the real world data.
        /// </summary>
        private void checkState() {
            lock (_updatingMapsLock) {            
                if (notifyReloadMaps) {
                    maps = mapsTemp;
                    mapsTemp = null;
                    notifyReloadMaps = false;
                }
            }
            lock (_updatingUserlistLock) {
                if (notifyReloadUserlist) {
                    userlist = userlistTemp;
                    userlistTemp = null;
                    notifyReloadUserlist = false;
                }
            }
        }



        /// <summary>
        /// Gets the download link for the specified map name.
        /// </summary>
        /// <param name="mapname">The mapname.</param>
        /// <returns></returns>
        public static string GetDownloadLink(string mapname) {
            return DOWNLOAD_LINK + mapname.Replace(" ", "%20");
        }



        /// <summary>
        /// Synchronizes the Map List into mapsTemp and notifies furkiebot to reload. Notifies the IRC channels about any state changes that have happened.
        /// </summary>
        private void SyncMapList() {
            lock (_updatingMapsLock) {
                if (maps != null) {
                    Dictionary<string, MapData> tocheck = DeserializeMaps(cmrId);
                    Dictionary<string, MapData> tocompare = maps;

                    if (mapsTemp != null) { tocompare = mapsTemp; } //in case we stored map changes in between furkiebot loop executions.
                    acceptedCount = 0;
                    pendingCount = 0;
                    string message;
                    foreach (KeyValuePair<string, MapData> entry in tocheck) {     // Notifies IRC about any state changes that have happened and sets the flag to reload maps.
                        string key = entry.Key;
                        MapData data = entry.Value;
                        if (entry.Value.accepted) {
                            acceptedCount++;
                        } else {
                            pendingCount++;
                        }
                        if (tocompare.ContainsKey(key)) {
                            if (tocompare[key].accepted == false && data.accepted == true) { // map has been approved.
                                acceptedCount++;
                                pendingCount--;
                                message = "Map \"" + data.name + "\" by " + data.author + " approved by " + data.acceptedBy;
                                MsgChans(message);
                                notifyReloadMaps = true;
                            } else if (tocompare[key].accepted == true && data.accepted == false) {  // map has been unapproved.
                                pendingCount++;
                                acceptedCount--;
                                message = "Map \"" + data.name + "\" by " + data.author + " removed from approved map list";
                                MsgChans(message);
                                MsgTesters(message + ". Download " + GetDownloadLink(data.name) + SEP + " Approve at " + TEST_LINK);
                                notifyReloadMaps = true;
                            } else if (tocompare[key].timestamp != data.timestamp) { // map has been resubmitted
                                if (tocompare[key].accepted == true) {  // resubmit of an approved map
                                    pendingCount++;
                                    acceptedCount--;
                                    data.accepted = false;
                                    message = "Map \"" + data.name + "\" by " + data.author + " removed from approved map list";
                                    MsgChans(message);
                                    MsgTesters(message + ". Download " + GetDownloadLink(data.name) + SEP + " Approve at " + TEST_LINK);
                                } else { // is a resubmit of a pending map
                                    message = "Map \"" + data.name + "\" by " + data.author + " resubmitted";
                                    MsgChans(message);
                                    MsgTesters(message + ". Download " + GetDownloadLink(data.name) + SEP + " Approve at " + TEST_LINK);
                                }
                                notifyReloadMaps = true;
                            }                            
                        } else { //Map is newly submitted
                            pendingCount++;
                            message = "Map \"" + data.name + "\" by " + data.author + " submitted for testing";
                            MsgChans(message);
                            MsgTesters(message + ". Download " + GetDownloadLink(data.name) + SEP + " Approve at " + TEST_LINK);
                            notifyReloadMaps = true;
                        }
                    }
                    if (notifyReloadMaps) {
                        mapsTemp = tocheck; // update the maps list to the new one.
                        Console.WriteLine("\nReloaded the map file.\n");
                    }
                } else {
                    Console.WriteLine("\n\n\nmaps is null. The fuck?\n\n");
                    maps = DeserializeMaps(cmrId);
                }
            }
        }



        /// <summary>
        /// Synchronizes the userlist.
        /// </summary>
        private void SyncUserList() {
            lock (_updatingUserlistLock) {
                if (userlist != null) {
                    Dictionary<string, PlayerInfo> tocheck = getUserList();
                    Dictionary<string, PlayerInfo> tocompare = userlist;

                    if (userlistTemp != null) { tocompare = userlistTemp; }
                        //nothing to compare atm

                    userlistTemp = tocheck; // update the maps list to the new one.
                    SyncOtherTables();
                    Console.WriteLine("\nReloaded the userlist file.\n");
                } else {
                    Console.WriteLine("\n\n\nUserlist is null. The fuck?\n\n");
                    userlist = getUserList();
                }
            }
        }



        /// <summary>
        /// Calling this causes FurkieBot to connect via the configuration that FurkieBot was initialized with.
        /// </summary>
        public void Connect() {
            try {
                IRCConnection = new TcpClient(config.server, config.port);
            } catch {
                Console.WriteLine("Connection Error");
                throw;
            }

            try {
                ns = IRCConnection.GetStream();
                bs = new BufferedStream(ns);
                sr = new StreamReader(bs);
                sw = new StreamWriter(ns);
                sendData("USER", config.nick + " 0 * :" + config.name);
                sendData("NICK", config.nick);
                sendData("PASS", config.pass);
            } catch {
                Console.WriteLine("Communication error");
                throw;
            }
        }  /* Connect() */




        /// <summary>
        /// Sends the provided command and parameters to the IRC server.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <param name="param">The parameter.</param>
        public void sendData(string cmd, string param) {
            lock (_sendingMessageLock) {
                if (param == null) {
                    sw.WriteLine(cmd);
                    sw.Flush();
                    Console.WriteLine(cmd);
                } else {
                    if (param.Length > MAX_MSG_LENGTH) //Makes sure to send multiple messages in case a message is too long for irc
                {
                        Console.WriteLine("TOO LONG, param = \"" + param + "\"");
                        string channel = "";
                        param = param.Trim();
                        //if (param[0] == '#') {   //if message being sent to a channel, not a user
                        channel = param.Substring(0, param.IndexOf(" ")) + " ";

                        param = param.Remove(0, channel.Length);
                        //}

                        string ss = param;
                        int size = ss.Length / MAX_MSG_LENGTH;
                        string[] newParam = new string[size + 1];

                        for (int i = 0; i < size + 1; i++) {
                            newParam[i] = ss.Substring(0, Math.Min(ss.Length, MAX_MSG_LENGTH - 50));

                            if (i != size)
                                ss = ss.Remove(0, MAX_MSG_LENGTH - 50);

                            if (i != 0) {
                                string lastword = newParam[i - 1].Substring(newParam[i - 1].LastIndexOf(' ') + 1);
                                string firstword = newParam[i].Substring(0, newParam[i].IndexOf(" "));

                                if (lastword != "" && firstword != "") {
                                    newParam[i] = newParam[i].Insert(0, lastword);
                                    newParam[i - 1] = newParam[i - 1].Remove(MAX_MSG_LENGTH - 50 - (lastword.Length + 1), lastword.Length + 1);
                                }

                            }
                        }
                        for (int i = 0; i < size + 1; i++) {
                            sw.WriteLine(cmd + " " + channel + newParam[i]);
                            sw.Flush();
                            Console.WriteLine(cmd + " " + channel + newParam[i]);
                        }
                    } else {
                        sw.WriteLine(cmd + " " + param);
                        sw.Flush();
                        Console.WriteLine(cmd + " " + param);
                    }
                }
            }
        }  /* sendData() */




        /// <summary>
        /// Determines whether the specified nick is registered.
        /// </summary>
        /// <param name="nick">The nickname.</param>
        /// <returns>bool whether or not the nick is registered.</returns>
        private bool IsRegistered(string nick) {
            if (userlist.ContainsKey(nick)) {
                if (userlist[nick].password != "") {
                    return true;
                } else {
                    //sendData("NOTICE", nick + " :You'll need to register your nick with FurkieBot before you may do this. Type .help register for more info.");
                    return false;
                }
            } else {
                //sendData("NOTICE", nick + " :You'll need to register your nick with FurkieBot before you may do this. Type .help register for more info.");
                return false;
            }
        }



        /// <summary>
        /// Determines whether the specified nick is an admin.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="toNotify">The IRC user initiating this check.</param>
        /// <returns>bool whether or not the nick is an admin.</returns>
        public bool IsAdmin(string nick, string toNotify) {
            nick = nick.ToLower();
            if (IsRegistered(nick) && IsIdentified(nick, toNotify)) {
                return userlist[nick].admin;
            } else return false;
        }




        /// <summary>
        /// Determines whether the specified nick is a tester.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="toNotify">To notify.</param>
        /// <returns></returns>
        public bool IsTester(string nick, string toNotify) {
            if (IsIdentified(nick, toNotify)) {
                if (IsRegistered(nick)) {
                    return userlist[nick].tester;
                } else {
                    return false;
                }
            } else if (IsRegistered(nick)) {
                return false;
            } else {
                return false;
            }
        }



        /// <summary>
        /// Determines whether the specified nick is trusted.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="toNotify">To notify.</param>
        /// <returns></returns>
        public bool IsTrusted(string nick, string toNotify) {
            if (IsIdentified(nick, toNotify)) {
                if (IsRegistered(nick)) {
                    return userlist[nick].trusted;
                } else {
                    return false;
                }
            } else {
                return false;
            }
        }




        /// <summary>
        /// Determines whether the specified nick is identified.
        /// </summary>
        /// <param name="nick">The nick.</param>
        /// <param name="toNotice">To notice.</param>
        /// <returns>Whether or not the nick is identified.</returns>
        public bool IsIdentified(string nick, string toNotice) {
            lock (_whoisLock) {
                if (identlist.ContainsKey(nick) && identlist[nick]) {
                    Console.WriteLine("Successfully identified " + nick);
                    return true;
                } else {

                    if (!complexAllowed) {
                        NoticeRetry(toNotice);
                    } else {
                        complexAllowed = false;
                        sendData("WHOIS", nick);

                        bool isIdentified = false;
                        bool is318 = false;

                        while (!is318) {

                            string[] ex;
                            string data;

                            data = sr.ReadLine();
                            Console.WriteLine(" ");
                            char[] charSeparator = new char[] { ' ' };
                            ex = data.Split(charSeparator, 5);
                            Console.WriteLine("Waiting on whois for " + nick + ", ex[1] = " + ex[1]);

                            if (ex.Length > 3 && ex[3].ToLower() == ":register") {
                                Console.WriteLine("Password info hidden");
                            } else {
                                Console.WriteLine("IsIdentified: " + data);
                            }
                            if (ex[1] == "307") {
                                isIdentified = true;
                                Console.WriteLine("Successfully identified " + nick);
                                is318 = true;
                            } else if (ex[1] == "318") {
                                is318 = true;
                            } else {
                                ProcessInput(ex, data, charSeparator);
                            }

                            //Console.WriteLine("End Last Switch " + parseTimer.Elapsed);
                        }
                        complexAllowed = true;
                        if (identlist.ContainsKey(nick)) {
                            identlist[nick] = isIdentified;
                        } else {
                            identlist.Add(nick, isIdentified);
                        }
                        return isIdentified;
                    }
                    return false;
                }
            }           
        }




        /// <summary>
        /// Attempts to set the Users password to the given password. 
        /// If no user exists by this nickname, create a new user.
        /// </summary>
        /// <param name="nickname">The nickname.</param>
        /// <param name="password">The password.</param>
        private void AttemptRegistration(string nickname, string password) {
            string nickLower = nickname.ToLower();
            if (IsIdentified(nickLower, nickname)) {
                string[] hashes = GeneratePasswordHashes(password.Trim());
                if (userlist.ContainsKey(nickLower)) {
                    PlayerInfo info = userlist[nickLower];
                    info.salt = hashes[0];
                    info.password = hashes[1];
                    info.ircname = nickname;
                    //userlist.Remove(nickname);
                    //userlist.Add(nickname, info);
                    userlist[nickLower] = info;
                    WriteUsers();
                } else {
                    PlayerInfo info = new PlayerInfo();
                    info.salt = hashes[0];
                    info.password = hashes[1];
                    info.ircname = nickname;
                    info.dustforcename = "";
                    info.streamurl = "";
                    info.admin = false;
                    info.tester = false;
                    info.trusted = false;
                    userlist[nickLower] = info;
                    WriteUsers();
                }
                Notice(nickname, "Successfully registered your nick with FurkieBot! Dont forget your password. You can always re-register if you forget the password.");
                Notice(nickname, "You will now want to set your in-game dustforce name with FurkieBot. use \".setign <steam / drm-free name>\" to set your IGN with FurkieBot.");
            } else {
                NoticeNotIdentified(nickname);
            }
        }








        /// <summary>
        /// Resets all users' tester status to false.
        /// </summary>
        private void ResetTesters() {
            Dictionary<string, PlayerInfo> newUserList = new Dictionary<string, PlayerInfo>(userlist.Count * 2);
            foreach (KeyValuePair<string, PlayerInfo> entry in userlist) {
                PlayerInfo info = entry.Value;
                info.tester = false;
                newUserList[entry.Key] = info;
            }
            userlist = newUserList;
            WriteUsers();
            SyncOtherTables();
            MsgChans("All testers have been reset to non testers. If you want to be a tester for the next CMR use \".settester true\" provided you have tester permissions. If you try this and don't have permissions, ask an admin.");
        }



        /// <summary>
        /// Sets the upcoming CMR number to the specified number.
        /// </summary>
        /// <param name="cmrNum">The CMR number to set to.</param>
        private void SetCMR(int cmrNum) {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(DATA_PATH + @"CMR_ID.txt")) {
                file.WriteLine(cmrNum.ToString());
            }
            cmrId = cmrNum;
            InitDirectories();
        }




        /// <summary>
        /// Notifies the privided nick that they are not identified.
        /// </summary>
        /// <param name="nick">The user to notify.</param>
        private void NoticeNotIdentified(string nick) {
            sendData("NOTICE", nick + " :Sorry, you need to first be using a Nickname registered on SRL. ");
            sendData("NOTICE", nick + " :\"/msg NickServ HELP REGISTER\".");
        }




        /// <summary>
        /// Notifies the privided nick that they are not registered.
        /// </summary>
        /// <param name="nick">The user to notify.</param>
        private void NoticeNotRegistered(string nick) {
            sendData("NOTICE", nick + " :Sorry, you need to register with FurkieBot first! \".help register\"");
        }




        /// <summary>
        /// Notifies the privided nick that they need to retry the command in a moment.
        /// </summary>
        /// <param name="nick">The user to notify.</param>
        private void NoticeRetry(string nick) {
            sendData("NOTICE", nick + " :Sorry, FurkieBot was processing something complex. Try that command again!");
        }




        /// <summary>
        /// Sends a message to the provided channel.
        /// </summary>
        /// <param name="chan">The channel to message.</param>
        /// <param name="message">The message to send to the channel.</param>
        private void Msg(string chan, string message) {
            sendData("PRIVMSG", " " + chan + " :" + message);
        }



        /// <summary>
        /// Sends a message to all currently joined channels.
        /// </summary>
        /// <param name="message">The message to send to the channel.</param>
        private void MsgChans(string message) {
            sendData("PRIVMSG", mainchannel + " :" + message);
            sendData("PRIVMSG", cmrchannel + " :" + message);
            //sendData("PRIVMSG", realRacingChan + " :" + message);
        }



        /// <summary>
        /// Sends a notice to the provided user.
        /// </summary>
        /// <param name="user">The user to message.</param>
        /// <param name="message">The message to send to the channel.</param>
        private void Notice(string user, string message) {
            sendData("NOTICE", user + " :" + message);
        }




        /// <summary>
        /// Sends a message to all testers.
        /// </summary>
        /// <param name="toSay">The message.</param>
        private void MsgTesters(string toSay) {
            List<string> testers = GetTesters();
            foreach (string tester in testers) {
                //Console.WriteLine("\n\n\nTester: " + tester + " ");
                //Console.WriteLine("is identified? " + identified + ".\n\n\n");
                Msg(tester.Trim(), toSay);
            }
        }




        /// <summary>
        /// Sends a notice to all testers.
        /// </summary>
        /// <param name="toSay">The message.</param>
        private void NoticeTesters(string toSay) {
            List<string> testers = GetTesters();
            foreach (string tester in testers) {
                //Console.WriteLine("\n\n\nTester: " + tester + " ");
                //Console.WriteLine("is identified? " + identified + ".\n\n\n");
                Notice(tester.Trim(), toSay);
            }
        }



        /// <summary>
        /// The main loop for IRC work.
        /// </summary>
        public void IRCWork() {
            bool shouldRun = true;

            while (shouldRun) {
                Console.WriteLine(" ");

                string[] ex;
                string data;

                data = sr.ReadLine();
                checkState();


                char[] charSeparator = new char[] { ' ' };
                ex = data.Split(charSeparator, 5);

                if (ex.Length > 3 && ex[3].ToLower() == ":register") {
                    Console.WriteLine("Password info hidden");
                } else {
                    Console.WriteLine("Outer: " + data);
                }
                shouldRun = ProcessInput(ex, data, charSeparator);

                //Console.WriteLine("End Last Switch " + parseTimer.Elapsed);
            }
        }




        /// <summary>
        /// Processes input from the IRC server. Massive switch statement for now.
        /// </summary>
        /// <param name="input">The string array of split input from the IRC server.</param>
        /// <param name="data">The whole string unsplit from IRC server.</param>
        /// <param name="charSeparator">The character separator. Why am I passing this in? No idea.</param>
        /// <returns>A boolean indicating whether or not to continue running.</returns>
        private bool ProcessInput(string[] input, string data, char[] charSeparator) {
            bool shouldRun = true;

            //Just some Regex bullshit to get username from full name/hostname shit
            string inputt = input[0];
            string re22 = "((?:[a-z][a-z0-9_]*))";
            Regex rr = new Regex(re22, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match usernamee = rr.Match(inputt);
            string nick = usernamee.ToString();
            string nickLower = nick.ToLower();

            string op = input[1];
            string chan = "";
            if (input.Length > 2) {
                chan = input[2].Trim();
            }
            if (chan.ToLower() == BOT_NAME.ToLower()) { //allows furkiebot to consider PM's as a "channel"
                chan = nick;
            }




            if (input[0] == "PING") //Pinging server in order to stay connected
                {
                sendData("PONG", input[1]);
            }




            //Events
            switch (op) {
                case "001": //Autojoin channel when first response from server
                    sendData("JOIN", mainchannel);
                    Msg("NickServ", "ghost " + config.nick + " " + config.pass);
                    sendData("JOIN", cmrchannel);

                    //OutputMapStatus(null);
                    break;
                case "433": //Changes nickname to altNick when nickname is already taken
                    //sendData("NICK", config.altNick);
                    break;
                case "353": //NAMES command answer from server
                    if (comNames == "CANCEL") //Kick all irc users from channel
                        {
                        int amount = CountCertainCharacters(data, ' ') - 5;
                        string r = input[4].Substring(input[4].IndexOf(@":") + 1);
                        string[] name = r.Split(charSeparator, amount);
                        foreach (string s in name) {
                            char[] gottaTrimIt = new char[] { '@', '+', '%' };
                            string n = s.Trim().TrimStart(gottaTrimIt);
                            if (n != BOT_NAME) {
                                sendData("KICK", realRacingChan + " " + n);
                            }
                        }
                        sendData("PART", realRacingChan);
                        dummyRacingChan = "#cmr-";
                        realRacingChan = "";
                        comNames = "";
                    }
                    break;
                case "JOIN": //Message someone when they join a certain channel
                    #region
                    if (chan == ":" + realRacingChan) {
                        if (IsRegistered(nickLower)) {
                            if (getUserIgn(nickLower) == "") { 
                                // if they have no IGN set.
                                Notice(nick, "You need to set your Dustforce name with FurkieBot in order to join a race. Type " + BoldText(".setign dustforcename") + " to register the name you use in Dustforce");
                            } else {                          
                                // let them know what their IGN currently is.
                                Notice(nick, "Your Dustforce name is currently set to " + ColourChanger(getUserIgn(nickLower), "03") + ". If your name has changed, please set a new nickname using " + BoldText(".setign dustforcename"));
                            }
                            if (CheckEntrant(nickLower)) {
                                sendData("MODE", realRacingChan + " +v " + nick);
                            }
                        } else {
                            NoticeHelpRegister(nick);
                        }
                    }

                    if (chan == ":" + mainchannel) {//Event: When someone joins main channel
                        if (cmrStatus == "open") {//Message sent to someone that joins the main channel, notifying that there's a CMR open at the moment
                            Notice(nick, "Entry currently " + ColourChanger("OPEN", "03") + " for Custom Map Race " + cmrId + ". Join the CMR at " + ColourChanger(realRacingChan, "04") + ". " + CountEntrants() + " entrants");
                        }
                        if (cmrStatus == "racing") {//Message sent to someone that joins the main channel, notifying that there's a CMR going on at the moment
                            Notice(nick, "Custom Map Race " + cmrId + " is currently " + ColourChanger("In Progress", "12") + " at " + ColourChanger(realRacingChan, "04") + ". " + CountEntrants() + " entrants");
                        }
                    }
                    break;
                    #endregion

                case "PART": //Update identlist on someone leaving channel
                    #region
                    if (identlist.ContainsKey(nickLower)) {
                        identlist[nickLower] = false;
                    }
                    break;
                    #endregion

                case "QUIT": //Update identlist on QUIT
                    #region
                    if (identlist.ContainsKey(nickLower)) {
                        identlist[nickLower] = false;
                    }
                    break;
                    #endregion


                case "NICK": //Message someone when they join a certain channel
                    #region
                    //DISABLED DUE TO PERMORMANCE ISSUES
                    if (identlist.ContainsKey(nickLower)) {
                        identlist[nickLower] = false;
                    }
                    break;
                    #endregion
                //default:
                //    break;
            }



            //Console.WriteLine("End event switch " + parseTimer.Elapsed);



            CheckInput(chan, nick, input, data);

               
            

            string command = ""; //grab the command sent

            if (input.Length > 3) {
                command = input[3];
            }

            if (input.Length == 4) {//Commands without parameters

                switch (command.ToLower()) {

                    case ":.help":
                    case ":.commands":
                    case ":.commandlist":
                    case ":.furkiebot": //FurkieBot Commands
                        if (!StringCompareNoCaps(chan, realRacingChan)) {
                            //FurkieBot commands for the main channel
                            Msg(chan, @"Commands: .cmr" + SEP + ".maps" + SEP + ".startcmr" + SEP + ".ign " + BoldText("ircname") + SEP + ".setign " + BoldText("in-game name") + SEP + ".mappack" + SEP + ".pending" + SEP + ".accepted");
                            Msg(chan, @"Upload maps: " + UPLOAD_LINK + SEP + "CMR info: " + INFO_LINK + SEP + @"Command list: " + WIKI_LINK + SEP + "FurkieBot announce channel: #DFcmr");
                            Msg(chan, @".help register" + SEP + ".help tester" + SEP + ".help othercommands");

                        } else {            // FurkieBot commands for race channel
                            Msg(chan, @"Command list: " + SEP + ".enter  .unenter" + SEP + ".ready  .unready" + SEP + ".setign " + BoldText("dustforceName") + SEP + ".setstream " + BoldText("URL") + SEP + ".entrants" + SEP + ".register" + SEP + ".streams" + SEP + @"More commands: https://github.com/EklipZgit/furkiebot/wiki");
                            Msg(chan, "Commands during a race:" + SEP + ".done" + SEP + ".undone" + SEP + ".forfeit" + SEP + ".ign " + BoldText("IRCname") + SEP + ".setign " + BoldText("in-game name"));                    
                        }
                        break;

                    case ":.register":
                        NoticeHelpRegister(nick);
                        break;

                    case ":.cmr": //General CMR FAQ
                        #region
                        //goto case ":.cmrmaps";
                        OutputCMRinfo(chan, cmrtime, cmrtimeString, nick);
                        break;
                        #endregion

                    case ":.startcmr+": // Used for testing purposes, forces the start of a race without having to worry about the date and time, make sure to use this command when mainchannel is NOT #dustforce
                        if (IsAdmin(nickLower, nick)) {
                            cmrtime = DateTime.Now.TimeOfDay;
                            goto case ":.startcmr";
                        }
                        break;
                    case ":.startcmr": //Opening that CMR hype
                        #region
                        if (cmrStatus == "closed") {
                            // Veryfying whether it is Saturday and if the time matches with CMR time
                            DateTime saturday;
                            if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday) {
                                if (cmrtime.ToString(@"%h\:mm\:ss") == cmrtimeString) {
                                    saturday = GetNextDateForDay(DateTime.Now, DayOfWeek.Saturday).Date;
                                } else {
                                    saturday = DateTime.Now.Date;
                                }
                            } else {
                                saturday = DateTime.Now.Date;
                            }
                            DateTime cmrday = saturday.Date + cmrtime;
                            TimeSpan duration = cmrday - DateTime.Now;
                            string nextCmrD = duration.Days.ToString();
                            string nextCmrH = duration.Hours.ToString();
                            string nextCmrM = duration.Minutes.ToString();
                            string nextCmrS = duration.Seconds.ToString();

                            if (maps.Count < MIN_MAPS && cmrtime.ToString(@"%h\:mm\:ss") == "10:30:00") {// If there are less than MIN_MAPS maps submitted AND if command wasn't issued using .startcmr+
                                Msg(chan, "There are not enough maps to start a CMR. We need " + (MIN_MAPS - maps.Count) + " more maps to start a CMR.");
                            } else {
                                TimeSpan stopTheTime = new TimeSpan(20, 29, 20);
                                DateTime stopTheSpam = saturday.Date + stopTheTime;
                                if (DateTime.Now < cmrday && DateTime.Now > stopTheSpam) {
                                    Msg(chan, "I get it, I can start a racechannel very soon. Jeez, stop spamming already (??;)");
                                }
                                if (DateTime.Now < cmrday && DateTime.Now < stopTheSpam) {
                                    Msg(chan, "We have enough maps to start Custom Map Race " + cmrId + ", race channel can be " + BoldText("opened") + " in "
                                        + ColourChanger(nextCmrD + " days, "
                                        + nextCmrH + " hours, "
                                        + nextCmrM + " minutes and "
                                        + nextCmrS + " seconds", "03") + ". The race may be " + BoldText("started") + " " + ColourChanger("30 minutes","04") + " after it can be opened.");
                                }
                                if (DateTime.Now >= cmrday) {
                                    if (IsAdmin(nickLower, nick)) {
                                        StartCmr();
                                    } else {
                                        Msg(chan, "Only an Admin can start the race for now, please contact an admin to start the race.");
                                    }
                                }
                            }
                        } else {
                            Msg(chan, "Custom Map Race " + cmrId + " has already been iniatied. Join " + realRacingChan + " to participate.");
                        }
                        break;
                        #endregion

                    case ":.cancelcmr": //Shattering everyones dreams by destroying that CMR hype
                        #region
                        if (IsAdmin(nickLower, nick)) {
                            if (cmrStatus == "open" || cmrStatus == "finished" || cmrStatus == "racing") {
                                if (chan == realRacingChan) {
                                    cmrStatus = "closed";
                                    Msg(chan, "Custom Map Race " + cmrId + " has been cancelled by " + nick + ".");
                                    Msg(mainchannel, "Custom Map Race " + cmrId + " has been cancelled.");
                                    Msg(cmrchannel, "Custom Map Race " + cmrId + " has been cancelled.");
                                    //for (int i = 0; i < CountEntrants(racers); i++)
                                    //{
                                    //    string name2DeVoice = racers.Rows[i]["Name"].ToString();
                                    //    sendData("MODE", realRacingChan + " -v " + name2DeVoice);
                                    //}
                                    EndCmr();
                                } else {
                                    Msg(chan, "" + "A race can only be cancelled in the CMR racing channel " + realRacingChan);
                                }
                            }
                        }
                        break;
                        #endregion

                        #region MAPS
                    case ":.maps": //Shows a list of currently approved maps
                    case ":.cmrmaps":
                        OutputMapStatus(chan);
                        break;

                    case ":.pending":
                        OutputPending(chan);
                        break;

                    case ":.approved":
                    case ":.accepted":
                        OutputAccepted(chan);
                        break;
                        #endregion

                    case ":.entrants": //Shows a list of the users currently in a race
                        #region
                        if (chan == realRacingChan) {
                            //Command only works in racing channel
                            Msg(chan, GetEntrantString());
                        }
                        #endregion
                        break;
                        
                        #region JOIN RACE
                    case ":.join": //Get that fool in the CMR hype
                    case ":.enter":
                        if (chan == realRacingChan) {
                            //Command only works in racing channel
                            if (cmrStatus == "open") {
                                //Command only works if CMR is open
                                if (IsRegistered(nickLower)) {
                                    if (getUserIgn(nickLower) != "+") {
                                        if (!CheckEntrant(nickLower)) {//Command only works if user isn't part of the race
                                            //Add user to race
                                            AddEntrant(nickLower);
                                            string extraS = "";
                                            if (CountEntrants() > 1) {
                                                extraS = "s";
                                            }
                                            Msg(chan, nick + " (" + getUserIgn(nickLower) + ") enters the race! " + CountEntrants() + " entrant" + extraS + ".");
                                            sendData("MODE", realRacingChan + " +v " + nick);
                                        } else {
                                            Msg(chan, nick + " already entered the race.");
                                        }
                                    } else {
                                        Msg(chan, "No ign registered.");
                                    }
                                } else { //user not registered
                                    Notice(nickLower, "You are not registered, you need to register with FurkieBot to enter the race. Type \".register\" for more information.");
                                }
                            } else {
                                Msg(chan, "CMR not open for registration.");
                            }
                        }
                        #endregion
                        break;
                        
                        #region UN JOIN RACE
                    case ":.unjoin": //Remove that fool from the CMR hype
                    case ":.unenter":
                        if (chan.ToLower() == realRacingChan.ToLower()) {
                            //Command only works in racing channel
                            if (cmrStatus == "open") {
                                //Command only works if CMR is open
                                if (GetStatus(nickLower) == 6 || GetStatus(nickLower) == 3) {//Command only works if racer status is "standby" or "ready"
                                    //Remove user from race
                                    RemoveEntrant(nickLower);
                                    string extraS = "";
                                    if (CountEntrants() > 1 || CountEntrants() == 0) {
                                        extraS = "s";
                                    }
                                    Msg(chan, "" + nick + " has been removed from the race. " + CountEntrants() + " entrant" + extraS + ".");
                                    sendData("MODE", realRacingChan + " -v " + nick);
                                }
                                if (ComfirmMassStatus(3) && racers.Rows.Count > 1) {
                                    goto case ":.go";
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.ready": //Gotta get them ready for the upcoming CMR maps
                        #region
                        if (chan == realRacingChan) { 
                            //Command is only possible in racing channel
                            if (cmrStatus == "open") { 
                                //Command is only possible if CMR is open
                                if (GetStatus(nickLower) == 6) { 
                                    //Comment only works if racer status is "standby"
                                    //Set racer status to "ready"
                                    SetStatus(nickLower, 3);
                                    int notReadyCount = CountEntrants() - CountStatus(3);
                                    Msg(chan, "" + nick + " is ready. " + notReadyCount + " remaining.");
                                }
                                if (ComfirmMassStatus(3) && racers.Rows.Count > 1) {
                                    goto case ":.go";
                                }
                            }
                        }
                        #endregion
                        break;


                    case ":.streams": //Gotta get them ready for the upcoming CMR maps
                        #region
                        if (chan == realRacingChan) { //Command is only possible in racing channel
                            if (racers.Rows.Count > 0) {
                                string streamsOut = "Entrants streams: ";
                                for (int i = 0; i < racers.Rows.Count; i++) {
                                    if (getStream(racers.Rows[i]["Name"].ToString().ToLower()) != "http://www.twitch.tv" && getStream(racers.Rows[i]["Name"].ToString().ToLower()) != "") {
                                        streamsOut = streamsOut + SEP + getStream(racers.Rows[i]["Name"].ToString().ToLower());
                                    }
                                }
                                Msg(realRacingChan, streamsOut);
                            }
                        }
                        #endregion
                        break;

                    case ":.unready": //NO WAIT IM NOT READY YET
                        #region
                        if (chan == realRacingChan) { 
                            //Command only works in racing channel
                            if (cmrStatus == "open") { 
                                //Command only works if CMR is open
                                if (GetStatus(nickLower) == 3) { 
                                    //Command only works if CMR is openmand only works if racer status is "ready"
                                    //Set racer status to "standby"
                                    SetStatus(nickLower, 6);
                                    int notReadyCount = CountEntrants() - CountStatus(3);
                                    Msg(chan, nick + " is not ready. " + notReadyCount + " remaining.");
                                }
                            }
                        }
                        #endregion
                        break;
                        
                    case ":.quit":
                        #region QUIT / FORFEIT RACE
                    case ":.forfeit":
                        if (chan == realRacingChan) { 
                            //Command only works in racing channel
                            if (cmrStatus == "racing") { 
                                //Command only works if CMR is open
                                if (GetStatus(nickLower) == 2) { 
                                    //Command only works if racer status is "racing"
                                    //Set racer status to "quit"
                                    SetStatus(nickLower, 4);
                                    Msg(chan, nick + " has forfeited from the race.");
                                    if (ComfirmDoubleMassStatus(4, 5)) {
                                        //Stop the race if all racers are "quit"/"dq"
                                        StopRace(stahpwatch);
                                        cmrStatus = "finished";
                                        sendData("TOPIC", realRacingChan + " " + ":Status: Complete | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                        Msg(mainchannel, "Race Finished: Dustforce - Custom Map Race " + cmrId + " | No one was able to finish the race. The race ended at " + GetTimeRank(1));
                                    } else {
                                        if (ComfirmTripleMassStatus(1, 4, 5)) {
                                            //Stop the race if all racers are "done"/"quit"/"dq"
                                            StopRace(stahpwatch);
                                            cmrStatus = "finished";
                                            sendData("TOPIC", realRacingChan + " " + ":Status: Complete | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                            Msg(mainchannel, "Race Finished: Dustforce - Custom Map Race " + cmrId + " | " + GetNameRank(1) + " won with a time of " + GetTimeRank(1));
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.startrace":
                    case ":.go": //Starts race and timer
                        #region
                        if (chan == realRacingChan) { //Command only works in racing channel
                            if (cmrStatus == "open") { //Command only works if CMR is open
                                if (CountEntrants() > 1) { //Command only works if there is at least 1 racer
                                    if (ComfirmMassStatus(3)) { //Command only works if all racers have status on "ready"
                                        bool valid = true;
                                        foreach (MapData md in maps.Values) {
                                            if (md.id <= 0) valid = false;
                                        }
                                        if (valid) {    // All maps have an atlas link recognized
                                            checker.StopChecking();
                                            cmrStatus = "racing";
                                            Msg(chan, BoldText(ColourChanger("The race will begin in 10 seconds!", "04")));
                                            Thread.Sleep(5000);
                                            Msg(chan, BoldText(ColourChanger("5", "04")));
                                            Thread.Sleep(990);
                                            Msg(chan, BoldText(ColourChanger("4", "04")));
                                            Thread.Sleep(990);
                                            Msg(chan, BoldText(ColourChanger("3", "04")));
                                            Thread.Sleep(990);
                                            Msg(chan, BoldText(ColourChanger("2", "04")));
                                            Thread.Sleep(990);
                                            Msg(chan, BoldText(ColourChanger("1", "04")));
                                            Thread.Sleep(990);
                                            StartRace(racers, stahpwatch);
                                            Msg(chan, BoldText(ColourChanger("GO!", "04")));
                                            sendData("TOPIC", realRacingChan + " " + ":Status: IN PROGRESS | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                        } else {
                                            Msg(chan, "There are still some maps whose atlas ID's have not been linked. Use .setmap <atlas ID #> <map name> or .removemap <mapname> to remove it from the race.");
                                        }
                                        
                                    } else {
                                        Msg(chan, "Not everyone is ready yet.");
                                    }
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.time": //Shows elapsed time in CMRs
                        #region
                        if (chan == realRacingChan) { 
                            //Command only works in racing channel
                            if (cmrStatus == "racing" || cmrStatus == "finished") { 
                                //Command only works if CMR is open or finished
                                Msg(chan, "" + GetTime(stahpwatch));
                            }
                        }
                        #endregion
                        break;

                    case ":.done": //When someone gets an SS on every CMR map
                        #region
                        if (chan == realRacingChan) { 
                            //Command only works in racing channel
                            if (cmrStatus == "racing") { 
                                //Command only works if CMR is racing
                                if (GetStatus(nickLower) == 2) { //Command only works if racer status is "racing"
                                    //Set racer status to "done"
                                    SetTime(nickLower, stahpwatch);
                                    //Msg("TRAXBUSTER", ".proofcall " + getUserIgn(nickLower));
                                    Msg(chan, nick + " has finished in " + GetRanking(nickLower) + " place with a time of " + GetTime(stahpwatch) + ".");
                                    if (ComfirmTripleMassStatus(1, 4, 5)) {
                                        //Stop the race if all racers are "done"/"quit"/"dq"
                                        //Set race status to "finished"
                                        StopRace(stahpwatch);
                                        cmrStatus = "finished";
                                        sendData("TOPIC", realRacingChan + " " + ":Status: Complete | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                        Msg(mainchannel, "Race Finished: Dustforce - Custom Map Race " + cmrId + " | " + GetNameRank(1) + " won with a time of " + GetTimeRank(1));
                                    }
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.undone": //Not quite done or continue racing after quitting
                        #region
                        if (chan == realRacingChan) { //Command only works in racing channel
                            if (cmrStatus == "racing") { //Command only works if CMR is open
                                if (GetStatus(nickLower) == 1 || GetStatus(nickLower) == 4) { //Command only works if racer status is "done" or "quit"
                                    //Set racer status to "racing"
                                    SetStatus(nickLower, 2);
                                    Msg(chan, "" + nick + " isn't done yet.");
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.record": //Used to record a race, outputting the final results in .xlsx
                        #region
                        if (IsAdmin(nickLower, nick)) {
                            if (cmrStatus == "finished") {
                                Msg(chan, "Recording race...");
                                RecordResultsReddit(cmrId);
                                //RecordResultsJson(UpdateJsonToDtMaps(cmrId.ToString()), cmrId);
                                cmrStatus = "closed";
                                Msg(chan, "Custom Map Race " + cmrId + " has been succesfully recorded by " + nick + ".");
                                sendData("TOPIC", realRacingChan + " " + ":Status: Closed | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                Msg(chan, "This channel will be cleared in 5 seconds.");
                                Thread.Sleep(5000);
                                EndCmr();
                                ResetCmr();
                            }
                        }
                        #endregion
                        break;

                    case ":.mappack":
                    case ":.mappacks":
                        #region
                        Msg(chan, "Download map packs here: " + MAP_PACK_LINK);
                        #endregion
                        break;

                    case ":.faq":
                        Msg(chan, @"Wiki: " + WIKI_LINK);
                        break;
                    case ":.hype":
                        if (!hype) {
                            Msg(chan, "GET HYPE!!!!!");
                            hype = true;
                        } else {
                            Msg(chan, "THE HYPE IS REAL!");
                        }
                        break;
                    case ":.unhype":
                        if (hype) {
                            Msg(chan, "Aww :c");
                            hype = false;
                        }
                        break;

                    case ":.switchchannel": //Used to switch main channels, not really recommended to use, just make sure the right channel is properly hard coded, can be used for very quick tests
                        if (mainchannel == "#dustforce") {
                            mainchannel = "#dustforcee";
                            sendData("PART", "#dustforce");
                            sendData("JOIN", "#dustforcee");
                        } else {
                            mainchannel = "#dustforce";
                            sendData("PART", "#dustforcee");
                            sendData("JOIN", "#dustforce");
                        }
                        break;
                    #region PING
                    case ":.ping":
                        Msg(chan, "pong");
                        break;
                    case ":.pong":
                        Msg(chan, "ping");
                        break;
                    #endregion
                    case ":.updatebot": //doesn't actually update anything, just shuts down Furkiebot with a fancy update message, I always whisper this because it would look stupid to type a command like this in channel lol
                        if (IsAdmin(nickLower, nick)) {
                            sendData("QUIT", " Updating FurkieBot (????)");
                        }
                        break;
                    case ":.adminlist":
                    case ":.admins":
                        OutputAdmins(chan);
                        break;
                    case ":.testerlist":
                    case ":.testers":
                        OutputTesters(chan);
                        break;
                    case ":.trustedlist":
                    case ":.trusted":
                    case ":.trusteds":
                        OutputTrusted(chan);
                        break;
                    case ":.molly":
                        Msg(chan, MOLLY);
                        break;
                    //case ":.pastas":
                    //case ":.pasta":
                    //    PastaNoParam(nick, chan);
                    //    break;

                }
            }
            //Console.WriteLine("End no-params command switch " + parseTimer.Elapsed);





            


            string parameter = "";
            string paramLower = "";


            if (input.Length > 4) {
                parameter = input[4].Trim();
                paramLower = parameter.ToLower();
            }


            if (input.Length > 4) { //Commands with parameters

                switch (command.ToLower()) {
                    case ":.help":
                        #region
                        switch (paramLower) {
                            case "register":
                                NoticeHelpRegister(nick);
                                break;
                            case "tester":
                                if (!IsIdentified(nickLower, nick)) {
                                    NoticeNotIdentified(nick);
                                }
                                Msg(chan, "Tester Commands:" + SEP + "Test maps at " + TEST_LINK + SEP + "To accept via IRC \".accept <mapAuthor>-<mapName>\"" + SEP + "\".unaccept <mapAuthor>-<mapName>\"");
                                Msg(chan, ".settester <trueOrFalse> -  sets you as a DEDICATED tester for the next CMR. This is not undoable.");
                                Msg(chan, "To be a map tester you must currently be a trusted community member. Ask a FurkieBot administrator if you cannot use .settester and believe you should be able to.");
                                break;
                            case "othercommands":
                                Msg(chan, "Other Commands:" + SEP + "\".setnotify <trueORfalse>\" to be notified when the CMR race channel opens" + SEP + "\".slap <name>\" is a useless command to slap people");
                                break;
                        }

                        break;
                        #endregion

                    case ":.j61": // oin #channel
                        sendData("JOIN", parameter);
                        break;

                    case ":.p61": //Part #channel
                        sendData("PART", parameter);
                        break;

                    case ":.deleteign":
                    case ":.freeign":
                    case ":.removeign":
                        if (IsAdmin(nickLower, nick)) {
                            removeIGN(parameter, nick);
                        } else {
                            Notice(nick, "Ask an admin to perform this for you.");
                        }
                        break;

                    case ":.ign":
                        #region
                        string ign_ex4 = parameter;
                        if (StringCompareNoCaps(ign_ex4, getUserInfo(ign_ex4.ToLower()).ircname)) {
                            Msg(chan, ColourChanger(parameter, "03") + " > " + ColourChanger(getUserInfo(ign_ex4).dustforcename, "03") + "");
                        } else {
                            Msg(chan, "No in-game name registered for " + parameter + "");
                        }
                        #endregion
                        break;

                    case ":.setign":
                        #region
                        if (IsRegistered(nickLower)) {
                            if (IsIdentified(nickLower, nick)) {
                                if (setUserIGN(nick, parameter)) { 
                                    Msg(chan, "New IGN registered: " + ColourChanger(nick, "03") + " > " + ColourChanger(parameter, "03") + ""); 
                                }
                            } else {
                                NoticeNotIdentified(nick);
                            }
                        } else {
                            NoticeNotRegistered(nick);
                        }

                        #endregion
                        break;
                    case ":.setnotify":
                    case ":.notify":
                        #region

                        if (IsRegistered(nickLower)) {
                            if (IsIdentified(nickLower, nick)) {
                                if (StringCompareNoCaps(parameter, "on") || StringCompareNoCaps(parameter, "true")) {
                                    setUserNotify(nickLower, true);
                                    Msg(chan, nick + " will be notified via IRC when the CMR channel opens.");
                                } else if (StringCompareNoCaps(parameter, "off") || StringCompareNoCaps(parameter, "false")) {
                                    setUserNotify(nickLower, false);
                                    Msg(chan, nick + " will NOT be notified via IRC when the CMR channel opens.");
                                } else {
                                    Msg(chan, "Syntax is \".notify " + BoldText("false/true") + "\"");
                                    break;
                                }
                            } else { NoticeNotIdentified(nick); }
                        } else { NoticeNotRegistered(nick); }
                        #endregion
                        break;
                                
                                


                    case ":.comment": //Adds a comment after a racer is done
                        #region
                        if (GetStatus(nickLower) == 1 || GetStatus(nickLower) == 4) {
                            AddComment(nickLower, parameter);
                            Msg(chan, "Comment for " + nick + " added.");
                        }
                        #endregion
                        break;

                    //case ":.dq": //DQ's a racer from race, should hardly be used, especially in combination with TRAXBUSTER, unless someone is clearly being a dick or something
                    //    #region
                    //    if (chan == realRacingChan) { //Command only works in racing channel
                    //        if (IsAdmin(nickLower, nick)) {

                    //            DQEntrant(parameter, nickLower);
                    //            Msg(chan, "" + nick + " disqualified PLACEHOLDER for reason: PLACEHOLDER");
                    //            if (ComfirmTripleMassStatus(1, 4, 5)) { //Stop the race if all racers are "done"/"quit"/"dq"
                    //                //Set race status to "finished"
                    //                StopRace(stahpwatch);
                    //                cmrStatus = "finished";
                    //                sendData("TOPIC", realRacingChan + " :" + "Status: Complete" + SEP + "Game: Dustforce" + SEP + "Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                    //                sendData("PRIVMSG", mainchannel + " :" + "Race Finished: Dustforce - Custom Map Race " + cmrId + SEP + GetNameRank(1) + " won with a time of " + GetTimeRank(1));
                    //            }
                    //        }
                    //    }
                    //    #endregion
                    //    break;

                    case ":.setcmr": //Set CMR ID for whatever reason there might 
                        #region
                        if (IsAdmin(nickLower, nick)) {
                            if (int.TryParse(parameter, out cmrId)) {
                                ResetTesters();
                                SetCMR(cmrId);
                                Msg(chan, "Custom Map Race has been set to " + parameter);
                                InitDirectories();
                            }
                        }

                        #endregion
                        break;

                    case ":.quit61": //Quit 
                        #region
                        if (IsAdmin(nickLower, nick)) {
                            sendData("QUIT", parameter);
                            shouldRun = false; //turn shouldRun to false - the server will stop sending us data so trying to read it will not work and result in an error. This stops the loop from running and we will close off the connections properly
                        }
                        #endregion
                        break;
                    case ":.removemap":
                    case ":.deletemap":
                    case ":.delmap": //Not sure if this works, used to remove a map from the .cmrmaps command list
                        #region
                        if (IsAdmin(nickLower, nick) || IsTester(nickLower, nick)) {
                            if (DeleteMap(parameter.ToLower(), nick)) {
                                Msg(chan, "Map removed.");
                            } else {
                                Msg(chan, "Map doesn't exist.");
                            }
                        }
                        #endregion
                        break;

                    //case ":.editmapid": //
                    //    #region
                    //    if (true) {
                    //        int i = parameter.Split(',').Length - 1; //Count amount of commas

                    //        if (i == 2 && IsAdmin(nickLower, nick)) {
                    //            string s = ",";

                    //            /*
                    //             * This is what I use to assign a mapid to an approved map. Since FurkieBot doesnt know how to get approved maps from Atlas, this is the way I do it. 
                    //             * [int mapid] should not be used once there's a better system for map submission.
                    //            */
                    //            int mapid = Convert.ToInt32(StringSplitter(parameter, s)[0]);
                    //            string mapper = StringSplitter(parameter, s)[1];
                    //            string mapname = StringSplitter(parameter, s)[2];

                    //            EditCMRMapId(cmrId, mapid, mapper, mapname);

                    //            sendData("NOTICE", nick + @" http://" + "atlas.dustforce.com/" + mapid + " > \"" + mapname + "\" by " + mapper);
                    //        } else {
                    //            sendData("NOTICE", "Furkiepurkie" + " mapid,mapper,mapname");
                    //        }
                    //    }
                    //    #endregion
                    //    break;

                    case ":.forceunjoin": //You can force someone to .unjoin, please dont abuse your powers unless you are a troll
                        #region
                        if (IsAdmin(nickLower, nick)) {
                            RemoveEntrant(paramLower);
                            string extraS = "";
                            if (CountEntrants() != 1) {
                                extraS = "s";
                            }
                            Msg(chan, "" + nick + " removed " + parameter + " from the race. " + CountEntrants() + " entrant" + extraS + ".");
                            sendData("MODE", realRacingChan + " -v " + parameter);
                        }
                        #endregion
                        break;

                    case ":.forcequit": //You can force someone to .quit, please dont abuse your powers unless you are a troll
                        #region
                        if (IsAdmin(nickLower, nick)) {
                            SetStatus(paramLower, 4);
                            Msg(chan, nick + " forced " + parameter + " to forfeit from the race.");
                        }
                        #endregion
                        break;

                    case ":.forcedone": //You can force someone to .done, because sometimes, you just want to be able to guarentee that
                        #region
                        if (IsAdmin(nickLower, nick)) {
                            //SetStatus(ex[4], 1);
                            SetTime(parameter, stahpwatch);

                            Msg(chan, parameter + " has finished in " + GetRanking(parameter) + " place with a time of " + GetTime(stahpwatch) + ".");
                            if (ComfirmTripleMassStatus(1, 4, 5)) { //Stop the race if all racers are "done"/"quit"/"dq"
                                //Set race status to "finished"
                                StopRace(stahpwatch);
                                cmrStatus = "finished";
                                sendData("TOPIC", realRacingChan + " :Status: Complete | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                Msg(mainchannel, "Race Finished: Dustforce - Custom Map Race " + cmrId + " | " + GetNameRank(1) + " won with a time of " + GetTimeRank(1));
                            }
                        }
                        #endregion
                        break;

                    case ":.forceundone": //You can force someone to .undone, get rekt thought you were done son?
                        #region
                        Console.WriteLine("Nickname: \t" + nick);
                        if (IsAdmin(nickLower, nick)) {
                            Console.WriteLine("chan: \t" + chan);
                            if (chan == realRacingChan || StringCompareNoCaps(chan, nick)) //Command only works in racing channel
                                {
                                Console.WriteLine("CMR status: \t" + cmrStatus);
                                if (cmrStatus == "racing") //Command only works if CMR is open
                                    {
                                    Console.WriteLine("Racer status: \t" + GetStatus(parameter));
                                    if (GetStatus(parameter) == 1 || GetStatus(parameter) == 4) //Command only works if racer status is "done" or "quit"
                                        {
                                        //Set racer status to "racing"

                                        if (StringCompareNoCaps(nick, "traxbuster")) {
                                            string realnickname = getUserIrc(parameter);
                                            SetStatus(realnickname, 2);
                                            Msg(realRacingChan, "Nice try, " + getUserIgn(parameter) + "! Try to .done when you have an SS on " + BoldText("all") + " maps. You have been put back in racing status.");
                                            Notice(realnickname, "If something went wrong and the proofcall is not justified, message Furkiepurkie about this issue.");
                                        } else if (IsAdmin(nickLower, nick)) {
                                            SetStatus(nickLower, 2);
                                            Msg(chan, parameter + " isn't done yet.");
                                        }
                                    }
                                    Console.WriteLine("Racer status: \t" + GetStatus(parameter));
                                }
                            }
                        }
                        #endregion
                        break;
                    case ":.accept"://aliases for accept map
                    case ":.approve":
                    case ":.approvemap":
                    case ":.acceptmap":
                        if (IsIdentified(nickLower, nick) && (IsTester(nickLower, nick) || IsAdmin(nickLower, nick))) {
                            string mapname = parameter;
                            if (ApproveMap(mapname, nickLower)) {
                                Msg(chan, "Map successfully accepted.");
                            } else {
                                Msg(chan, "Sorry, no map by the name \"" + mapname + "\". in the map list.");
                            }
                        } else if (!IsTester(nickLower, nick)) {
                            Notice(nick, "You need to be a tester, sorry.");
                        }
                        break;


                    case ":.unaccept": //aliases for unaccept map
                    case ":.unapprove":
                    case ":.unapprovemap":
                    case ":.unacceptmap":
                        if (IsIdentified(nickLower, nick) && (IsTester(nickLower, nick) || IsAdmin(nickLower, nick))) {
                            string mapname = parameter;
                            if (File.Exists(MAPS_PATH + cmrId + "\\accepted\\" + mapname)) {
                                File.Move(MAPS_PATH + cmrId + "\\accepted\\" + mapname, MAPS_PATH + cmrId + "\\pending\\" + mapname);
                                Notice(nick, "Map successfully moved back to pending.");
                            } else {
                                Msg(chan, "Sorry, no file by the name \"" + mapname + "\". in accepted maps. Please .unaccept <mapMakerName>-<mapName>.");
                            }
                        } else if (!IsTester(nickLower, nick)) {
                            Notice(nick, "You need to be a tester, sorry.");
                        }
                        break;

                    case ":.settester":
                        if (IsAdmin(nickLower, nick)) {  //admin is sending this command
                            char[] separator = { ' ' };
                            string[] split = parameter.Split(separator, 2);
                            if (split.Length == 2 && (split[1].ToLower() == "true" || split[1].ToLower() == "false")) {    // name true/false
                                if (IsRegistered(split[0].ToLower())) {
                                    setTester(split[0].ToLower(), split[1]);
                                    Notice(split[0], "Your tester status set to: " + split[1]);
                                    Notice(nick, split[0] + "'s tester status set to: " + split[1]);
                                    if (!IsTrusted(split[0].ToLower(), nick) && split[1] == "true") {
                                        setTrusted(split[0].ToLower(), split[1]);
                                        Notice(split[0], "You are now trusted.");
                                    }
                                } else {
                                    Notice(nick, "That person isn't registered.");
                                }
                            } else {
                                Notice(nick, "Incorrect format. Use <name> <trueOrFalse>");
                            }
                        } else {
                            if (IsTrusted(nickLower, nick)) {//trusted
                                if (parameter.ToLower() == "true") {
                                    setTester(nickLower, parameter);
                                    Notice(nick, "Your tester status set to true. visit " + TEST_LINK + " to review the rules and test maps.");
                                } else if (paramLower == "false") {

                                    if (IsAdmin(nickLower, nick)) { //admin wants to set himself to not-tester
                                        setTester(nickLower, parameter);
                                        Notice(nick, "As you wish, sir. You are no longer a tester.");
                                    } else {                        //normal tester wants to set himself to not-tester
                                        Notice(nick, "Once you are a tester, you must remain a tester until after the next CMR. Ask a furkiebot admin after the CMR has ended to switch you off of tester.");
                                    }
                                } else {
                                    Notice(nick, "Bad format. Use true or false.");
                                }
                            } else { //not trusted
                                Notice(nick, "You are not allowed to be a tester. To get testing privileges, talk to a FurkieBot administrator.");
                            }
                        }
                        break;

                    case ":.setstream":
                        char[] separ = { ' ' };
                        string[] splitStream = parameter.Split(separ, 2);
                        if (IsAdmin(nickLower, nick) && splitStream.Length > 1) {  //admin is sending this command
                            string streamURL = splitStream[1].Trim();
                            streamURL = FormatStreamURL(streamURL);
                            string streamNick = splitStream[0].Trim();
                            if (IsRegistered(streamNick.ToLower())) {
                                setStream(streamNick.ToLower(), streamURL);
                                Msg(chan, streamNick + "'s stream url set to: " + streamURL);
                            } else {
                                Notice(nick, "That user isnt registered.");
                            }
                        } else {
                            string streamURL = splitStream[0];
                            streamURL = FormatStreamURL(streamURL);
                            if (IsIdentified(nickLower, nick)) {
                                if (IsRegistered(nickLower)) {
                                    setStream(nickLower, streamURL);
                                    Msg(chan, nick + "'s stream url set to: " + streamURL);
                                } else {
                                    NoticeNotRegistered(nick);
                                }
                            } else {
                                NoticeNotIdentified(nick);
                            }
                        }
                        break;

                    case ":.stream":
                        if (userlist.ContainsKey(paramLower)) {
                            Msg(chan, parameter + "'s stream url is: " + userlist[paramLower].streamurl);
                        }
                        break;

                    case ":.settrusted":
                        if (IsAdmin(nickLower, nick)) {
                            char[] separator = { ' ' };
                            string[] split = parameter.Split(separator, 2);
                            if (split.Length == 2 && (split[1].ToLower() == "true" || split[1].ToLower() == "false")) {
                                if (IsRegistered(split[0].ToLower())) {
                                    setTrusted(split[0].ToLower(), split[1]);
                                    Notice(split[0], "Your trusted status set to: " + split[1]);
                                    Notice(nick, split[0] + "'s trusted status set to: " + split[1]);
                                } else {
                                    Notice(nick, "That person isn't registered.");
                                }
                            } else {
                                Notice(nick, "Incorrect format. Use <name> <trueOrFalse>");
                            }
                        }
                        break;

                    case ":.setadmin":
                        if (nickLower == "eklipz" || nickLower == "furkiepurkie") { //TODO replace with fullops and halfops or someshit?
                            char[] separator = { ' ' };
                            string[] split = parameter.Split(separator, 2);
                            if (split[0].ToLower() == "furkiepurkie" || split[0].ToLower() == "eklipz") {
                                Notice(split[0], nick + " just tried to set your admin status to: " + split[1]);
                                Notice(nick, "Nice try");
                            } else {
                                if (split.Length == 2 && (split[1].ToLower() == "true" || split[1].ToLower() == "false")) {
                                    if (IsRegistered(split[0].ToLower())) {
                                        setAdmin(split[0].ToLower(), split[1]);
                                        Notice(split[0], "Your admin status set to: " + split[1]);
                                        Notice(nick, split[0] + "'s admin status set to: " + split[1]);
                                    } else {
                                        Notice(nick, "That person isn't registered.");
                                    }
                                } else {
                                    Notice(nick, "Incorrect format. Use \".setadmin name trueOrFalse\"");
                                }
                            }
                        }
                        break;

                    case ":.setrating":
                        if (IsAdmin(nickLower, nick)) {
                            char[] separator = { ' ' };
                            string[] split = parameter.Split(separator, 2);
                            if (split.Length == 2) {
                                if (IsRegistered(split[0].ToLower())) {
                                    setRating(split[0].ToLower(), split[1]);
                                    Notice(split[0], "Your rating set to: " + split[1]);
                                    Notice(nick, split[0] + "'s rating set to: " + split[1]);
                                } else {
                                    Notice(nick, "That person isn't registered.");
                                }
                            } else {
                                Notice(nick, "Incorrect format. Use <name> <trueOrFalse>");
                            }
                        }
                        break;

                    case ":.setrandmaprating":
                        if (IsAdmin(nickLower, nick)) {
                            char[] separator = { ' ' };
                            string[] split = parameter.Split(separator, 2);
                            if (split.Length == 2) {
                                if (IsRegistered(split[0].ToLower())) {
                                    setRandmapRating(split[0].ToLower(), split[1]);
                                } else {
                                    Notice(nick, "That person isn't registered.");
                                }
                            } else {
                                Notice(nick, "Incorrect format. Use <name> <trueOrFalse>");
                            }
                        }
                        break;


                    case ":.maps":                                      // THE case for if a CMR ID number is provided.
                    case ":.cmrmaps":
                        #region  
                        //COMMENTED OUT UNTIL I HAVE BETTER IMPLEMENTATION FOR THIS SHIT. TODO
                        
                        //int value;
                        //if (int.TryParse(parameter, out value)) { //I dont remember why I need to parse here
                        //    DataTable dt = UpdateJsonToDtMaps(parameter);
                        //    string maps = GetCMRMaps(parameter, dt);
                        //    if (parameter != cmrId.ToString()) {
                        //        if (Convert.ToInt32(dt.Rows[0]["mapid"]) != -1) {
                        //            Msg(chan, "" + "Maps used in CMR " + parameter + " (" + dt.Rows.Count + "): " + maps);
                        //        } else {
                        //            Msg(chan, "" + "No maps found.");
                        //        }
                        //    } else {
                        //        if (Convert.ToInt32(dt.Rows[0]["mapid"]) != -1) {
                        //            Msg(chan, "" + "Maps approved for CMR " + cmrId + " (" + dt.Rows.Count + "/6): " + maps);
                        //        } else {
                        //            Msg(chan, "" + "No maps submitted yet.");
                        //        }
                        //    }
                        //}
                        #endregion
                        break;
                    case ":.say":
                        if (IsAdmin(nickLower, nick) && op == "PRIVMSG" && StringCompareNoCaps(chan, nick)) {
                            Msg(mainchannel, parameter);
                            Msg(cmrchannel, parameter);
                        }
                        break;
                    case ":.saydf": //Can be used to broadcast a message to the mainchannel by whispering this command to FurkieBot
                        if (IsAdmin(nickLower, nick) && op == "PRIVMSG" && StringCompareNoCaps(chan, nick)) {
                            Msg(mainchannel, parameter);
                        }
                        break;
                    case ":.sayrc":
                    case ":.sayracechan": //Can be used to broadcast a message to the racechannel by whispering this command to FurkieBot
                        if (IsAdmin(nickLower, nick) && op == "PRIVMSG" && StringCompareNoCaps(chan, nick)) {
                            Msg(realRacingChan, parameter);
                        }
                        break;

                    case ":.kick": //Kick someone from a racingchannel
                        if (chan == realRacingChan && IsAdmin(nickLower, nick)) {
                            sendData("KICK", chan + " :" + parameter);
                        }
                        break;

                    //case ":.test":
                    //    break;
                    //case ":.pastas":
                    //case ":.pasta": //pasta with parameters
                    //    PastaParam(nick, chan, parameter);
                    //    break;
                    #region .slap
                    case ":.slap": //A stupid command nobody asked for
                        Slap(nick, chan, parameter);
                        break;
                    #endregion


                    case ":register":
                        AttemptRegistration(nick, parameter);
                        break;

                }
            }
            return shouldRun;
        }



        /// <summary>
        /// Resets the CMR.
        /// </summary>
        private void ResetCmr() {
            mapsTemp = null;
            ResetTesters();
            SetCMR(cmrId + 1);
            maps = DeserializeMaps(cmrId);
            notifyReloadMaps = false;
        }



        private void CheckInput(string chan, string nick, string[] input, string data) {
            //FurkieBot responding to people talking about him
            if ((input.Length > 3 && (input[3].ToLower().Contains("furkiebot"))) || (input.Length > 4 && (input[4].ToLower().Contains("furkiebot"))) || (input.Length > 5 && (input[5].ToLower().Contains("furkiebot")))) 
            {
                furkiebotMentionCount++;
            }
            if (furkiebotMentionCount > MENTION_EVERY) {
                furkiebotMentionCount = 0;
                Msg(chan, "DONT TALK ABOUT ME LIKE I'M NOT HERE!");
            }



            ////FurkieBot responding to molly mentions
            //if (input.Length > 4 && (input[4].ToLower().Contains("molly"))) {
            //    Msg(chan, MOLLY);
            //}
        }

        private void StartCmr() {
            realRacingChan = "";
            dummyRacingChan += RandomCharGenerator(5, 1);
            realRacingChan = dummyRacingChan;


            busterThread = new Thread(() => StartTraxBuster(realRacingChan));
            busterThread.Start();

            sendData("JOIN", realRacingChan);
            cmrStatus = "open";
            MsgChans("Race initiated for Custom Map Race " + cmrId + ". Join " + ColourChanger(realRacingChan, "04") + " to participate.");
            sendData("TOPIC", realRacingChan + " :" + ":Status: Entry Open | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
            sendData("MODE", realRacingChan + " +t");
            Msg("TRAXBUSTER", ".join001 " + realRacingChan);
            NotifyCmrStarting();
            checker = new AtlasChecker(this);
            checker.StartChecking();
        }




        private void EndCmr() {
            comNames = "CANCEL";
            sendData("NAMES", realRacingChan);
            sendData("MODE", realRacingChan + " +im");
            racers.Clear();
            buster.NotifyExit();
        }



        private void StartRace() {
            checker.StopChecking();
        }


        /// <summary>
        /// Notifies the users who asked to be notified of the start of a CMR.
        /// </summary>
        private void NotifyCmrStarting(){
            foreach(KeyValuePair<string, PlayerInfo> entry in userlist) {
                if ( entry.Value.notify == true ) {
                    Notice(entry.Value.ircname, "The CMR channel has opened. You asked to be notified of this event. If you wish these messages to stop, please type \".notify false\"");
                }
            }
        }



        /// <summary>
        /// Meant to be called as its own thread, this thread initializes and creates a TraxBuster.
        /// ALWAYS CALL THIS IN A SEPARATE THREAD! WILL TOTALLY BLOCK FURKIEBOT OTHERWISE.
        /// </summary>
        public void StartTraxBuster(string racechan) {
            IRCConfig conf = new IRCConfig();
            conf.name = "TRAXBUSTER";
            conf.nick = "TRAXBUSTER";
            conf.port = 6667;
            conf.server = "irc2.speedrunslive.com";
            conf.pass = GetIRCPass();
            using (buster = new TraxBuster(conf, this)) {
                buster.Connect();
                buster.IRCWork();
            }
            Console.WriteLine("TraxBuster quit/crashed");
            Console.ReadLine();
        } /* StartTraxBuster */



        /// <summary>
        /// Gets an array of the nicks of users registered to test maps for this CMR.
        /// </summary>
        /// <returns>An array of the nicks of users registered to test maps for this CMR.</returns>
        private List<string> GetTesters() {
            List<string> testers = new List<string>();
            foreach (KeyValuePair<string, PlayerInfo> entry in userlist) {
                if (entry.Value.tester) {
                    testers.Add(entry.Value.ircname);
                }
            }
            return testers;
        }


        /// <summary>
        /// Gets an array of the nicks of users that are trusted.
        /// </summary>
        /// <returns>An array of the nicks of users who are currently trusted.</returns>
        private List<string> GetTrusted() {
            List<string> trustedlist = new List<string>();
            foreach (KeyValuePair<string, PlayerInfo> entry in userlist) {
                if (entry.Value.trusted) {
                    trustedlist.Add(entry.Value.ircname);
                }
            }
            return trustedlist;
        }


        /// <summary>
        /// Formats the provided string into a full twitch or hitbox url.
        /// </summary>
        /// <param name="streamURL">The provided portion of a url.</param>
        /// <returns>The final stream url.</returns>
        private string FormatStreamURL(string streamURL) {
            
            string streamLower = streamURL.ToLower();
            string finalURL = "";
            if (streamLower.StartsWith(@"http://") || streamLower.StartsWith(@"https://")) {//Already starts with http://
                return streamURL;
            } else {
                finalURL = @"http://";
                if (streamLower.StartsWith(@"www.")) {
                    return finalURL + streamURL;
                } else {
                    finalURL = finalURL + @"www.";
                }
                if (!streamLower.StartsWith(@"twitch.tv/") && !streamLower.StartsWith(@"hitbox.tv/")) {
                    finalURL = finalURL + @"twitch.tv/";
                }
                finalURL = finalURL + streamURL;
                return finalURL;
            }
        }


        private string getStream(string nickname) {
            if (userlist.ContainsKey(nickname.ToLower())) {
                return userlist[nickname.ToLower()].streamurl;
            } else {
                return "Error, " + nickname + " wasnt in the userlist.";
            }
        }

        private void NoticeHelpRegister(string nickname) {
            if (!IsIdentified(nickname, nickname)) {
                NoticeNotIdentified(nickname);
            }
            sendData("NOTICE", nickname + " :To register with FurkieBot the password does not need to be the same as your SRL password. This is the username and password that you will use in IRC and on the CMR website.");
            sendData("NOTICE", nickname + " :\"/msg FurkieBot REGISTER password\".");
            sendData("NOTICE", nickname + " :DO NOT FORGET THE /msg PART, YOU DONT WANT TO SEND YOUR PASSWORD TO THE WHOLE CHANNEL.");
        }

        private void setRandmapRating(string nickname, string rating) {
            throw new NotImplementedException();
            if (IsRegistered(nickname)) {
                PlayerInfo info = userlist[nickname];
                info.tester = (rating.ToLower() == "true" ? true : false);
                userlist[nickname] = info;
                WriteUsers();
            }
        }

        private void setRating(string nickname, string rating) {
            throw new NotImplementedException();
            if (IsRegistered(nickname)) {
                PlayerInfo info = userlist[nickname];
                info.tester = (rating.ToLower() == "true" ? true : false);
                userlist[nickname] = info;
                WriteUsers();
            }
        }

        private void setAdmin(string nickname, string tOrF) {
            if (IsRegistered(nickname)) {
                PlayerInfo info = userlist[nickname];
                info.admin = (tOrF.ToLower() == "true" ? true : false);
                userlist[nickname] = info;
                WriteUsers();
            }
        }

        private void setTrusted(string nickname, string tOrF) {
            if (IsRegistered(nickname)) {
                PlayerInfo info = userlist[nickname];
                info.trusted = (tOrF.ToLower() == "true" ? true : false);
                userlist[nickname] = info;
                WriteUsers();
            }
        }

        private void setTester(string nickname, string tOrF) {
            if (IsRegistered(nickname)) {
                PlayerInfo info = userlist[nickname];
                info.tester = (tOrF.ToLower() == "true" ? true : false);
                userlist[nickname] = info;
                WriteUsers();
            }
        }

        private void setStream(string nickname, string url) {
            if (IsRegistered(nickname)) {
                PlayerInfo info = userlist[nickname];
                info.streamurl = url;
                userlist[nickname] = info;
                WriteUsers();
            }
        }




        /// <summary>
        /// Outputs info about the current CMR
        /// </summary>
        /// <param name="chan">The channel.</param>
        /// <param name="cmrtime">The cmrtime.</param>
        /// <param name="cmrtimeString">The cmrtime string.</param>
        /// <param name="nickname">The nickname.</param>
        private void OutputCMRinfo(string chan, TimeSpan cmrtime, string cmrtimeString, string nickname) {
            //Veryfying whether it is Saturday and if the time matches with CMR time
            DateTime saturday;
            if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday) {
                if (cmrtime.ToString(@"%h\:mm\:ss") == cmrtimeString)
                    saturday = GetNextDateForDay(DateTime.Now, DayOfWeek.Saturday).Date;
                else
                    saturday = DateTime.Now.Date;
            } else {
                saturday = DateTime.Now.Date;
            }
            DateTime cmrday = saturday.Date + cmrtime;
            TimeSpan duration = DateTime.Now.Date + cmrtime - DateTime.Now;
            string nextCmrD = duration.Days.ToString();
            string nextCmrH = duration.Hours.ToString();
            string nextCmrM = duration.Minutes.ToString();
            string nextCmrS = duration.Seconds.ToString();

            if (CmrMapCount(cmrId) < MIN_MAPS) { //If there are less than 6 maps submitted
                Msg(chan, "" + "Upcoming race is Custom Map Race " + cmrId + ". There are only " + maps.Count + " maps currently accepted, and we need at least " + MIN_MAPS + ".");
                Msg(chan, "" + "It will happen on Saturday, " + saturday.Month + "-" + saturday.Day.ToString() + @" at 6:30 pm GMT  ( conversion to your time here: http://www.timebie.com/std/gmt.php?q=18.5 )");
            } else {
                Console.WriteLine(DateTime.Now.TimeOfDay + "\t" + DateTime.Now.Date.ToString("dddd"));
                if (DateTime.Now.TimeOfDay < cmrtime && DateTime.Now.Date.ToString("dddd") == "Saturday") { //If it isnt CMR time yet
                    Msg(chan, "" + "We have enough maps to start Custom Map Race " + cmrId + ", race can be initiated in "
                        + ColourChanger(nextCmrD + " days, "
                        + nextCmrH + " hours, "
                        + nextCmrM + " minutes and "
                        + nextCmrS + " seconds", "03") + ".");
                } else //If starting a race is possible
                                {
                    string extraS = "";
                    if (CountEntrants() > 1) {
                        extraS = "s";
                    }
                    if (cmrStatus == "closed") //CMR race not opened yet
                                    {
                        Msg(chan, "Custom Map Race " + cmrId + " is available.");
                    }
                    if (cmrStatus == "open") //CMR race opened
                                    {
                        Msg(chan, "Entry currently " + ColourChanger("OPEN", "03") + " for Custom Map Race " + cmrId + ". Join the CMR at " + ColourChanger(realRacingChan, "04") + ". " + CountEntrants() + " entrants" + extraS);
                    }
                    if (cmrStatus == "racing") //CMR race ongoing
                                    {
                        sendData("NOTICE", nickname + " :Custom Map Race " + cmrId + " is currently " + ColourChanger("In Progress", "12") + " at " + ColourChanger(realRacingChan, "04") + ". " + CountEntrants() + " entrant" + extraS);
                    }
                }
            }
        }




        private void PastaParam(string nick, string chan, string target) {
            int allowedPastas = 2;
            if (nick.ToLower() != lastPastaer.ToLower()) { //reset the slap limit because someone else slapped.
                lastPastaer = nick;
                repeatPastas = 0;
            } else {    //increment slap count from same user.
                repeatPastas++;
            }


            if (userlist.ContainsKey(target.ToLower())) {
                target = userlist[target.ToLower()].ircname;
            }


            if (repeatPastas < allowedPastas) {
                string pastafile = "";
                if (File.Exists("ParamPastas.txt")) {
                    pastafile = "ParamPastas.txt";
                } else if (File.Exists("..\\ParamPastas.txt")) {
                    pastafile = "..\\ParamPastas.txt";
                } else if (File.Exists("..\\..\\ParamPastas.txt")) {
                    pastafile = "..\\..\\ParamPastas.txt";
                } else if (File.Exists("..\\..\\..\\ParamPastas.txt")) {
                    pastafile = "..\\..\\..\\ParamPastas.txt";
                } else if (File.Exists("..\\..\\..\\..\\ParamPastas.txt")) {
                    pastafile = "..\\..\\..\\..\\ParamPastas.txt";
                } else {
                    Console.WriteLine("ParamPastas.txt not found!");
                    return;
                }
                string[] lines = File.ReadAllLines(pastafile);

                Random r = new Random();
                int choice = 1 + r.Next(lines.Length - 1);
                while (lines[choice - 1].Trim() != "") {
                    choice--;
                    if (choice == 0) {
                        choice = 1 + r.Next(lines.Length - 1);
                    }
                }
                while (choice < lines.Length && lines[choice].Trim() != "") {
                    lines[choice] = lines[choice].Replace("[S]", target).Replace("[TODAY]", DateTime.Now.ToString("M/d/yyyy")).Replace("[N]", nick);
                    Msg(chan, lines[choice]);
                    choice++;
                }
            }
        }




        private void PastaNoParam(string nick, string chan) {
            int allowedPastas = 2;
            if (nick.ToLower() != lastPastaer.ToLower()) { //reset the slap limit because someone else slapped.
                lastPastaer = nick;
                repeatPastas = 0;
            } else {    //increment slap count from same user.
                repeatPastas++;
            }


            if (repeatPastas < allowedPastas) {
                string pastafile = "";
                if (File.Exists("Pastas.txt")) {
                    pastafile = "Pastas.txt";
                } else if (File.Exists("..\\Pastas.txt")) {
                    pastafile = "..\\Pastas.txt";
                } else if (File.Exists("..\\..\\Pastas.txt")) {
                    pastafile = "..\\..\\Pastas.txt";
                } else if (File.Exists("..\\..\\..\\Pastas.txt")) {
                    pastafile = "..\\..\\..\\Pastas.txt";
                } else if (File.Exists("..\\..\\..\\..\\Pastas.txt")) {
                    pastafile = "..\\..\\..\\..\\Pastas.txt";
                } else {
                    Console.WriteLine("Pastas.txt not found!");
                    return;
                }
                string[] lines = File.ReadAllLines(pastafile);

                Random r = new Random();
                int choice = 1 + r.Next(lines.Length - 1);
                while (lines[choice - 1].Trim() != "") {
                    choice--;
                    if (choice == 0) {
                        choice = 1 + r.Next(lines.Length - 1);
                    }
                }
                while (choice < lines.Length && lines[choice].Trim() != "") {
                    Msg(chan, lines[choice].Replace("[TODAY]", DateTime.Now.ToString("M/d/yyyy")).Replace("[N]", nick));
                    choice++;
                }
            }
        }




        /// <summary>
        /// Slaps the specified nickname.
        /// </summary>
        /// <param name="nickname">The nickname.</param>
        /// <param name="chan">The channel.</param>
        /// <param name="nameToSlap">The parameter.</param>
        private void Slap(string nickname, string chan, string nameToSlap) {

            int allowedSlaps = 3;
            if (nickname.ToLower() != lastSlapper.ToLower()) { //reset the slap limit because someone else slapped.
                lastSlapper = nickname;
                repeatSlaps = 0;
            } else {    //increment slap count from same user.
                repeatSlaps++;
            }
            
            if (repeatSlaps < allowedSlaps) {
                Random r = new Random();
                int choice = r.Next(18);


                if (userlist.ContainsKey(nameToSlap.ToLower())) {
                    nameToSlap = userlist[nameToSlap.ToLower()].ircname;
                }

                if (nameToSlap.ToLower() == "furkiebot") {      //they told furkiebot to slap himself
                    nameToSlap = nickname;

                } else if (nameToSlap.ToLower() == "glados") {      //they told furkiebot to slap GLaDOS
                    Msg(chan, ACT + @"ACTION angrily beats " + nickname + " with a frozen drumstick." + ACT);
                    Msg(chan, "Why would you even suggest that, you heartless shell of a person?");

                } else if (nameToSlap == "me" || nickname.ToLower() == nameToSlap.ToLower()) {  //person is trying to slap themselves.
                    Msg(chan, ACT + @"ACTION uses " + nickname + "'s own hands to slap them. \"STOP HITTING YOURSELF, STOP HITTING YOURSELF!\"" + ACT);

                } else if (IsAdmin(nameToSlap.ToLower(), nickname)) {   //trying to slap an admin
                    Msg(chan, ACT + @"ACTION slaps " + nickname + ". Don't be like that!" + ACT);

                } else if (nameToSlap.ToLower() == "jerseymilker") {
                    Msg(chan, ACT + @"ACTION slaps " + nickname + ". We all know JerseyMilker is slow as hell... No need to bully him because of it!" + ACT);



                } else {    //proceed with normal slap handling.
                    switch (choice) {
                        case 0:
                            Msg(chan, ACT + @"ACTION slaps " + nameToSlap + " with " + nickname + "'s favorite game console." + ACT);
                            break;
                        case 1:
                            if (IsAdmin(nickname.ToLower(), nickname)) {
                                goto case 4;
                            } else {
                                Msg(chan, "Only cool people are allowed to .slap people. Go slap yourself, " + nickname + ".");
                            }

                            break;
                        case 2:
                            Msg(chan, ACT + @"ACTION slaps " + nameToSlap + " around, just a little." + ACT);
                            break;
                        case 3:
                            Msg(chan, ACT + @"ACTION slaps " + nameToSlap + " with vigor." + ACT);
                            break;
                        case 4:
                            if (IsAdmin(nickname.ToLower(), nickname)) {
                                Msg(chan, ACT + @"ACTION slaps " + nameToSlap + " with his cold, metal bot-hand." + ACT);
                            } else {
                                Msg(chan, "Only cool people are allowed to .slap people. Go slap yourself, " + nickname + ".");
                            }
                            break;
                        case 5:
                            if (IsAdmin(nickname.ToLower(), nickname)) {
                                goto case 6;
                            } else {
                                Msg(chan, ACT + @"ACTION slaps " + nickname + ". BE NICE." + ACT);
                            }
                            break;
                        case 6:
                            Msg(chan, ACT + @"ACTION slaps " + nameToSlap + " around with a trashbag." + ACT);
                            break;
                        case 7:
                            Msg(chan, ACT + @"ACTION winds up for a hefty open-handed smack to " + nameToSlap + "'s face." + ACT);
                            break;
                        case 8:
                            Msg(chan, ACT + @"ACTION smacks " + nameToSlap + " playfully on the butt." + ACT);
                            break;
                        case 9:
                            Msg(chan, ACT + @"ACTION slaps " + nameToSlap + " lazily. You can tell he's not that into it though." + ACT);
                            break;
                        case 10:
                            Msg(chan, ACT + @"ACTION stops to ponder the meaning of life. What does it all mean? Why do people want him to slap " + nameToSlap + "???" + ACT);
                            break;
                        case 11:
                            Msg(chan, ACT + @"ACTION refuses. " + nameToSlap + " would like that way too much..." + ACT);
                            break;
                        case 12:
                            Msg(chan, "Gross. I'm not touching that.");
                            break;
                        case 13:
                            if (IsAdmin(nickname.ToLower(), nickname)) {
                                goto case 7;
                            } else {
                                Msg(chan, "Wow. " + nickname + " is a jerk, are you all seeing this?");
                            }
                            break;
                        case 14:
                            Msg(chan, ACT + "ACTION chants: \"He's the F to the U, R-K-I-E-Bot, FurkieBot can slap you with just a thought.\"" + ACT);
                            Msg(chan, ACT + @"ACTION smacks " + nameToSlap + " with a resounding thud on the last note of the cheer." + ACT);
                            break;
                        case 15:
                            Msg(chan, MOLLY);
                            break;
                        case 16:
                            Msg(chan, "You do not have sufficient permissions to perform this action. You may need to run the program again with administrative rights.");
                            break;
                        case 17:
                            Msg(chan, nameToSlap + ", I THOUGHT YOU LOVED ME!");
                            Msg(chan, ACT + @"ACTION smacks " + nameToSlap + " while sobbing uncontrollably." + ACT);
                            break;
                    }
                }
            } else {
                return;
            }

            if (repeatSlaps == allowedSlaps - 1) {
                Msg(chan, ACT + @"ACTION slaps " + nickname + " as well, they better not be abusing .slap!" + ACT);
            }
        } /* Slap */


        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            if (sr != null)
                sr.Close();
            if (sw != null)
                sw.Close();
            if (ns != null)
                ns.Close();
            if (IRCConnection != null)
                IRCConnection.Close();
        } /* Dispose() */





        /// <summary>
        /// Gets the user information.
        /// </summary>
        /// <param name="ircuser">The ircuser.</param>
        /// <returns></returns>
        private PlayerInfo getUserInfo(string ircuser) {

            PlayerInfo res = new PlayerInfo();
            if (userlist.ContainsKey(ircuser.ToLower())) {
                userlist.TryGetValue(ircuser.ToLower(), out res);
                return res;
            }

            return res;
        }






        /// <summary>
        /// Gets the user ircname by the provided dustforceuser name.
        /// </summary>
        /// <param name="dustforceuser">The dustforceuser.</param>
        /// <returns></returns>
        private string getUserIrc(string dustforceuser) {
            PlayerInfo res = new PlayerInfo();
            if (dustforcelist.ContainsKey(dustforceuser.ToLower())) {
                dustforcelist.TryGetValue(dustforceuser.ToLower(), out res);
                return res.ircname;
            } else {
                return null;
            }
        }



        /// <summary>
        /// Gets the users in game dustforce name.
        /// </summary>
        /// <param name="ircuser">The nick of the user.</param>
        /// <returns>The users dustforce name.</returns>
        private string getUserIgn(string ircuser) {
            PlayerInfo res = new PlayerInfo();
            if (userlist.ContainsKey(ircuser.ToLower())) {
                userlist.TryGetValue(ircuser.ToLower(), out res);
                return res.dustforcename;
            } else {
                return null;
            }
        }



        /// <summary>
        /// Gets the user rating.
        /// </summary>
        /// <param name="ircuser">The ircuser.</param>
        /// <returns></returns>
        private int getUserRating(string ircuser) {
            PlayerInfo res = new PlayerInfo();
            if (userlist.ContainsKey(ircuser.ToLower())) {
                userlist.TryGetValue(ircuser.ToLower(), out res);
                return res.rating;
            } else {
                return -1;
            }
        }




        /// <summary>
        /// Sets a users in game dustforce name.
        /// </summary>
        /// <param name="ircuser">The ircuser whose name to set.</param>
        /// <param name="dustforceuser">The users dustforce name.</param>
        private bool setUserIGN(string ircuser, string dustforceuser) {
            string ircLower = ircuser.ToLower();
            //if (!|| (dustforcelist[dustforceuser].ircname.ToLower() == ircLower)) {
            if (dustforcelist.ContainsKey(dustforceuser) && dustforcelist[dustforceuser].ircname.ToLower() != ircuser.ToLower()) {
                Notice(ircuser, "That IGN is already registered to someone else. Perhaps you registered it under another IRC nickname? If this is an issue, ask an admin to use .deleteign on that IGN.");
                return false;
            } else {
                PlayerInfo temp = new PlayerInfo();
                userlist.TryGetValue(ircLower, out temp);
                string oldname = temp.dustforcename;
                //Console.WriteLine("name " + temp.ircname + " dustforcename " + temp.dustforcename + " tester " + temp.tester + " trusted " + temp.trusted + " admin " + temp.admin);

                //delete old dustforceuser entry
                if (oldname != null && dustforcelist.ContainsKey(oldname)) {
                    dustforcelist.Remove(oldname);
                }

                temp.dustforcename = dustforceuser;
                temp.ircname = ircuser;
                userlist[ircLower] = temp;
                dustforcelist.Add(dustforceuser, temp);

                WriteUsers();
                return true;
            }
        }


        /// <summary>
        /// Removes the provided IGN from any nicks that use it.
        /// </summary>
        /// <param name="ign">The ign.</param>
        private void removeIGN(string ign, string toNotify) {
            if (dustforcelist.ContainsKey(ign)) {
                string ircname = dustforcelist[ign].ircname.ToLower();
                PlayerInfo temp = userlist[ircname];
                temp.dustforcename = "";
                userlist[ircname] = temp;

                dustforcelist.Remove(ign);
            } else {
                Notice(toNotify, "That IGN isnt registered with FurkieBot.");
            }
        }
        
        
        /// <summary>
        /// Sets a user irc notify on/off
        /// </summary>
        /// <param name="ircuser">The ircuser whose name to set.</param>
        /// <param name="option">On or Off</param>
        private void setUserNotify(string ircuser, bool option) {
            string ircLower = ircuser.ToLower();
            PlayerInfo temp = new PlayerInfo();
            userlist.TryGetValue(ircLower, out temp);
            string oldoption = (temp.notify ? "true" : "false");
            
            temp.notify = option;
            userlist[ircLower] = temp;

            WriteUsers();
        }



        /// <summary>
        /// Serializes the user data to disk.
        /// </summary>
        private void WriteUsers() {
            if (userlist != null) {
                ignoreChangedUserlist = true;
                string json = JsonConvert.SerializeObject(userlist, Newtonsoft.Json.Formatting.Indented);

                File.WriteAllText(DATA_PATH + @"Userlist\userlistmap.json", json); // !! FILEPATH !!
            } else {
                throw new Exception("Null userlist attempting to be written");
            }
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




        /// <summary>
        /// Adds an entrant to the race.
        /// </summary>
        /// <param name="racer">The racer.</param>
        /// <returns></returns>
        private bool AddEntrant(string racer) //Used to add a racer to entrants
        {
            racers.Rows.Add(racer, 6, 0, 0, 0, 0, "", getUserRating(racer)); //name, status, hour, min, sec, 10th sec, comment, rating
            return true;
        } /* AddEntrant */


        



        private long CmrMapCount(int cmrid) //Used to count the amount of maps on the current CMR
        {
            Dictionary<string, MapData> maplist = DeserializeMaps(cmrid);
            return maplist.Count;
        } /* CountMapsInCMR() */




        static int GetCurrentCMRidFromFile() {//Used to fetch the current CMR number
            string[] id = System.IO.File.ReadAllLines(DATA_PATH + @"CMR_ID.txt"); // !! FILEPATH !!
            return Int32.Parse(id[0]);
        } /* GetCurrentCMRID() */


        
        static string GetCurrentCMRStatus() {//Used to fetch current CMR status
            string[] info = System.IO.File.ReadAllLines(DATA_PATH + @"CMR_STATUS.txt"); // !! FILEPATH !!
            return info[0];
        } /* GetCurrentCMRStatus */



        static void SetCurrentCMRStatus(string s) {//Used to either open or close a CMR
            string text = s;
            System.IO.File.WriteAllText(DATA_PATH + @"CMR_STATUS.txt", s); // !! FILEPATH !!
        } /* SetCurrentCMRStatus() */



        static string GetCMRMaps(string cmrid, DataTable dt) {//Used to get a certain line from the cmrmaps.txt file
            string res = "";

            //if (Convert.ToInt32(dt.Rows[0]["mapid"]) == -1) //If maps dont exist on a nonexistant cmrid
            //{
            //    res += "No maps.";
            //}
            //else
            //{
            bool firstMap = true;

            foreach (DataRow dr in dt.Rows) {
                if (!firstMap) // Makes sure seperator doesn't get placed before the first place
                {
                    res += ColourChanger(" | ", "07") + "\"" + dr["mapname"] + "\"" + " by " + dr["mapper"];
                } else {
                    res += "\"" + dr["mapname"] + "\"" + " by " + dr["mapper"];
                    firstMap = false;
                }
            }
            //}
            return res;
        } /* GetCMRMaps() */



        static long CountLinesInFile(string f) {//Used to count the amount of lines in a certain file
            long count = 0;
            using (StreamReader r = new StreamReader(f)) {
                string line;
                while ((line = r.ReadLine()) != null) {
                    count++;
                }
            }
            return count;
        } /* CountLinesInFile() */


        static string GetTime(Stopwatch s) //Fetches current time on stopwatch
        {
            TimeSpan ts = s.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}"/*.{3:00}*/,
                ts.Hours, ts.Minutes, ts.Seconds/*,
                ts.Milliseconds / 10*/);
            return elapsedTime;
        } /* GetTime() */



        static string GetTimeCountdown(Stopwatch s) {
            TimeSpan ts = s.Elapsed;
            string elapsedTime = String.Format(
                "{0:00}", ts.Seconds);
            return elapsedTime;
        }



        static int GetTimeTSec(Stopwatch s) {
            TimeSpan ts = s.Elapsed;
            int elapsedTime = ts.Milliseconds / 10;
            return elapsedTime;
        } /* GetTimeTSec */



        static int GetTimeSec(Stopwatch s) {
            TimeSpan ts = s.Elapsed;
            int elapsedTime = ts.Seconds;
            return elapsedTime;
        } /* GetTimeSec */



        static int GetTimeMin(Stopwatch s) {
            TimeSpan ts = s.Elapsed;
            int elapsedTime = ts.Minutes;
            return elapsedTime;
        } /* GetTimeMin */



        static int GetTimeHour(Stopwatch s) {
            TimeSpan ts = s.Elapsed;
            int elapsedTime = ts.Hours;
            return elapsedTime;
        } /* GetTimeHour */



        private void StartRace(DataTable racers, Stopwatch s) //Starts running timer
        {
            if (CountEntrants() > 0) {
                s.Start();
                foreach (DataRow dr in racers.Rows) {
                    dr["Status"] = 2;
                }
            }
        } /* StartTime()*/



        private void StopRace(Stopwatch timer) //Starts running timer
        {
            timer.Stop();
        } /* StopTime() */



        private string GetNameRank(int rank) {
            string res = "";
            if (CountEntrants() > 0) {
                DataView dv = racers.DefaultView;
                dv.Sort = "Status, Hour, Min, Sec, TSec";
                racers = dv.ToTable();
                res = racers.Rows[rank - 1]["Name"].ToString();
            }
            return res;
        }



        private string GetTimeRank(int rank) {
            string res = "";
            if (CountEntrants() > 0) {
                DataView dv = racers.DefaultView;
                dv.Sort = "Status, Hour, Min, Sec, TSec";
                racers = dv.ToTable();
                string hour = "";
                string min = "";
                string sec = "";

                //Making sure that single digits display as double digits
                int intHour = Convert.ToInt32(racers.Rows[rank - 1]["Hour"]); if (intHour < 10) { hour = "0" + intHour.ToString(); } else { hour = intHour.ToString(); }
                int intMin = Convert.ToInt32(racers.Rows[rank - 1]["Min"]); if (intMin < 10) { min = "0" + intMin.ToString(); } else { min = intMin.ToString(); }
                int intSec = Convert.ToInt32(racers.Rows[rank - 1]["Sec"]); if (intSec < 10) { sec = "0" + intSec.ToString(); } else { sec = intSec.ToString(); }

                res += hour + ":";
                res += min + ":";
                res += sec;

                Console.WriteLine(res + " h:" + hour + " m:" + min + " s:" + sec);
            }
            return res;
        }



        private int CountEntrants() //Used to count the amount entrants in the current CMR
        {
            return racers.Rows.Count;
        } /* CountEntrants() */



        private string GetEntrantString() {//Used to get one single string of entrants
            string result = "";
            if (CountEntrants() > 0) {
                DataView dv = racers.DefaultView;
                dv.Sort = "Status, Hour, Min, Sec, TSec, Name";
                racers = dv.ToTable();
                for (int i = 0; i < CountEntrants(); i++) {
                    string currentracer = "";
                    string name = racers.Rows[i]["Name"].ToString();
                    int status = Convert.ToInt32(racers.Rows[i]["Status"]);

                    //Making sure time shows up correctly
                    string realHour = ""; int hour = Convert.ToInt32(racers.Rows[i]["Hour"]); if (hour < 10) { realHour += "0" + hour.ToString(); } else { realHour = hour.ToString(); }
                    string realMin = ""; int min = Convert.ToInt32(racers.Rows[i]["Min"]); if (min < 10) { realMin += "0" + min.ToString(); } else { realMin = min.ToString(); }
                    string realSec = ""; int sec = Convert.ToInt32(racers.Rows[i]["Sec"]); if (sec < 10) { realSec += "0" + sec.ToString(); } else { realSec = sec.ToString(); }
                    string time = realHour + ":" + realMin + ":" + realSec;

                    if (status == 6) {
                        currentracer += name;
                    }
                    if (status == 5) {
                        currentracer += name + " (DQ)";
                        if (racers.Rows[i]["Comment"].ToString() != "") {
                            currentracer += " (" + racers.Rows[i]["Comment"].ToString() + ")";
                        }
                    }
                    if (status == 4) {
                        currentracer += name + " (forfeit)";
                        if (racers.Rows[i]["Comment"].ToString() != "") {
                            currentracer += " (" + racers.Rows[i]["Comment"].ToString() + ")";
                        }
                    }
                    if (status == 3) {
                        currentracer += name + " (ready)";
                    }
                    if (status == 2) {
                        currentracer += name + " (racing)";
                        if (racers.Rows[i]["Comment"].ToString() != "") {
                            currentracer += " (" + racers.Rows[i]["Comment"].ToString() + ")";
                        }
                    }
                    if (status == 1) {
                        currentracer += (i + 1) + ". " + name + " (" + time + ")";
                        if (racers.Rows[i]["Comment"].ToString() != "") {
                            currentracer += " (" + racers.Rows[i]["Comment"].ToString() + ")";
                        }
                    }
                    if ((i + 1) != CountEntrants()) //If currentracer isnt the last one on the list add a "|" and start over
                    {
                        currentracer += " | ";
                    }
                    result += currentracer;
                }
                return result;
            } else {
                return "There are no entrants.";
            }

        } /* GetEntrants */



        private bool CheckEntrant(string racer) //Checks if a certain user has entered the race
        {
            var foundRows = racers.Select("Name = '" + racer + "'");
            if (foundRows.Length != 0) {
                return true; //user found
            } else {
                return false; //user not found
            }
        } /* SearchEntrant */



        private void DQEntrant(string racerreason, string mod) {
            string txt = racerreason;

            string re1 = "((?:[a-z][a-z0-9_]*))";	// Variable Name 1

            Regex r = new Regex(re1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match m = r.Match(txt);
            string racer = m.ToString();
            string reason = mod + ": " + racerreason.Remove(0, racer.Length + 1);

            SetStatus(racer, 5);
            AddComment(racer, reason);
        }



        private int GetStatus(string racer) //
        {
            int status = 0;
            if (CheckEntrant(racer)) { //If racer exists in race
                for (int i = 0; i < CountEntrants(); i++) {
                    string name = racer;
                    if (racers.Rows[i]["Name"].ToString() == racer) {
                        status = Convert.ToInt32(racers.Rows[i]["Status"]);
                    }
                }
            }
            return status;
        }



        private bool ComfirmMassStatus(int status) { //Checks if the whole list of racers share the same status
            bool get = true;
            if (CountEntrants() > 0) {
                for (int i = 0; i < CountEntrants(); i++) {
                    int s = Convert.ToInt32(racers.Rows[i]["Status"]);
                    if (s != status) {
                        get = false;
                        break;
                    }
                }
            }
            return get;
        }



        private bool ComfirmDoubleMassStatus(int s1, int s2) //Checks if the whole list of racers share the same status
        {
            bool get = true;
            if (CountEntrants() > 0) {
                for (int i = 0; i < CountEntrants(); i++) {
                    int s = Convert.ToInt32(racers.Rows[i]["Status"]);
                    if (s != s1 && s != s2) {
                        get = false;
                        break;
                    }
                }
            }
            return get;
        }



        private bool ComfirmTripleMassStatus(int s1, int s2, int s3) { //Checks if the whole list of racers share the same status. 
            bool get = true;
            if (CountEntrants() > 0) {
                for (int i = 0; i < CountEntrants(); i++) {
                    int s = Convert.ToInt32(racers.Rows[i]["Status"]);
                    if (s != s1 && s != s2 && s != s3) {
                        get = false;
                        break;
                    }
                }
            }
            return get;
        }



        private bool SetStatus(string racer, int newStatus) //Sets status of a racer
        {
            var foundRows = racers.Select("Name = '" + racer + "'");
            if (foundRows.Length != 0) {
                foreach (DataRow dr in racers.Rows) {
                    if (dr["Name"].ToString() == racer) {
                        dr["Status"] = newStatus;
                    }
                }
                return true;
            } else {
                return false;
            }
        } /* SetReady */



        private int CountStatus(int status) {
            int count = 0;
            foreach (DataRow dr in racers.Rows) {
                if (Convert.ToInt32(dr["Status"]) == status) {
                    count++;
                }
            }
            return count;
        }



        private bool SetTime(string racer, Stopwatch timer) //Sets time on a racer that .done
        {
            if (CheckEntrant(racer)) //If user exists in race
            {
                foreach (DataRow dr in racers.Rows) {
                    if (dr["Name"].ToString() == racer && Convert.ToInt32(dr["Status"]) == 2) {
                        dr["TSec"] = GetTimeTSec(timer);
                        dr["Sec"] = GetTimeSec(timer);
                        dr["Min"] = GetTimeMin(timer);
                        dr["Hour"] = GetTimeHour(timer);
                        SetStatus(racer, 1);
                    }
                }
                return true;
            } else {
                return false;
            }
        } /* SetTime */



        private void RemoveEntrant(string racer) //Get that fool outta there
        {
            for (int i = 0; i < CountEntrants(); i++) {
                string name = racers.Rows[i]["Name"].ToString();
                if (name == racer) {
                    racers.Rows[i].Delete();
                }
            }
            racers.AcceptChanges();
        } /* RemoveEntrant */



        private string GetRanking(string racer) //Used to get proper ranks like 1st, 2nd, 3rd etc.
        {
            int r = 0;
            DataView dv = racers.DefaultView;
            dv.Sort = "Status, Hour, Min, Sec, TSec";
            racers = dv.ToTable();
            for (int i = 0; i < CountEntrants(); i++) {
                string name = racers.Rows[i]["Name"].ToString();
                if (name == racer) {
                    r = i + 1;
                }
            }
            int rest = 0;
            if (r < 10 && r > 14)
            {
                while (r > 10)
                {
                    r = r - 10;
                    rest += 10;
                }
            }
            int newrank = 0;
            string nr = "";
            if (r == 1) {
                newrank = r + rest;
                nr += newrank.ToString();
                nr += "st";
                return nr;
            }
            if (r == 2) {
                newrank = r + rest;
                nr += newrank.ToString();
                nr += "nd";
                return nr;
            }
            if (r == 3) {
                newrank = r + rest;
                nr += newrank.ToString();
                nr += "rd";
                return nr;
            } else {
                newrank = r + rest;
                nr += newrank.ToString();
                nr += "th";
                return nr;
            }
        }



        private void AddComment(string racer, string comment) {
            foreach (DataRow dr in racers.Rows) {
                if (dr["Name"].ToString() == racer) {
                    dr["Comment"] = comment;
                }
            }
        }



        static string[] StringSplitter(string s, string v) //Seperate a string in an array of strings
        {
            string[] separators = { @v };
            string value = @s;
            string[] words = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return words;
        } /* StringSplitter() */





        static DataTable UpdateFaqList() {
            string filepath = DATA_PATH + @"FAQ\faq.json"; // !! FILEPATH !!
            string[] jsonarray = File.ReadAllLines(filepath);
            string json = string.Join("", jsonarray);

            DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);

            DataTable dt = ds.Tables["faqlist"];

            return dt;
        }



        static string BoldText(string s) {
            string text = s;
            text = (char)2 + s + (char)2;
            return text;
        }



        static string ColourChanger(string s, string colour) //Used to colourcode text in irc
        {
            /* MIRC COLOURS           
             * 00 white            
             * 01 black            
             * 02 blue (navy)            
             * 03 green            
             * 04 red            
             * 05 brown (maroon)            
             * 06 purple            
             * 07 orange (olive)           
             * 08 yellow            
             * 09 light green (lime)            
             * 10 teal (a green/blue cyan)            
             * 11 light cyan (cyan) (aqua)
             * 12 light blue (royal)
             * 13 pink (light purple) (fuchsia)
             * 14 grey
             * 15 light grey (silver)
            */
            string text = s;
            text = (char)3 + colour + s + (char)3;
            return text;
        } /* ColourChanger */



        static int CountCertainCharacters(string s, char character) {
            int count = 0;
            foreach (char c in s)
                if (c == character) count++;
            return count;
        }



        static string RandomCharGenerator(int length, int type) //type 1 = chars and digits, type 2 = digits, type 3 = dice
        {
            string valid = "";
            if (type == 1) {
                valid = "abcdefghijklmnopqrstuvwxyz1234567890";
            }
            if (type == 2) {
                valid = "1234567890";
            }
            if (type == 3) {
                valid = "123456";
            }
            string res = "";
            Random rnd = new Random();
            while (0 < length--)
                res += valid[rnd.Next(valid.Length)];
            return res;
        } /* RandomChannelGenerator */



        static bool StringCompareNoCaps(string s1, string s2) {
            return string.Equals(s1, s2, StringComparison.CurrentCultureIgnoreCase);
        }



        static string GetCurrentDateTime(int id) //1 = Day; 2 = 24time; 3 = 12time
        {
            string res = "";
            DateTime thisDay = DateTime.Today;

            if (id == 1) {
                res = thisDay.ToString("dddd");
            }
            if (id == 2) {
                res = thisDay.ToString("HH:mm:ss");
            }
            if (id == 3) {
                res = thisDay.ToString("h:mm:ss tt");
            }
            return res;
        }



        static DateTime NextDay(DateTime from, DayOfWeek dayOfWeek) {
            int start = (int)from.DayOfWeek;
            int target = (int)dayOfWeek;
            if (target <= start)
                target += 7;
            return from.AddDays(target - start);
        }





        static string JsonToDatatableMaps2(DataTable dt, string cmrid, string irccommand, string ircchannel) {
            string res = "";

            if (File.Exists(DATA_PATH + @"CMR Results\CMR" + cmrid + "Results.json")) //Check if CMR number exists // !! FILEPATH !!
            {
                string[] path = System.IO.File.ReadAllLines(DATA_PATH + @"CMR Results\CMR" + cmrid + "Results.json"); // !! FILEPATH !!
                string json = string.Join("", path);

                JsonTextReader reader = new JsonTextReader(new StringReader(json));

                bool nextValueIsMapId = false;
                bool nextValueIsMapper = false;
                bool nextValueIsMap = false;

                bool firstRecord = true;

                while (reader.Read()) {
                    if (reader.Value != null) {
                        if (nextValueIsMap) {
                            res += reader.Value.ToString();
                            firstRecord = false;
                            nextValueIsMap = false;
                        }
                        if (reader.Value.ToString() == "mapname") {
                            nextValueIsMap = true;
                        }
                        if (nextValueIsMapper) {
                            if (firstRecord) {
                                res += ColourChanger(" > ", "07") + reader.Value + " - ";
                            } else {
                                res += ColourChanger(" > ", "07") + reader.Value + " - ";
                            }
                            nextValueIsMapper = false;
                        }
                        if (reader.Value.ToString() == "mapper") {
                            nextValueIsMapper = true;
                        }
                        if (nextValueIsMapId) {
                            if (firstRecord) {
                                res += ircchannel + " " + @" http://" + @"atlas.dustforce.com/" + reader.Value;
                            } else {
                                res += " \n" + irccommand + " " + ircchannel + " " + @" http://" + @"atlas.dustforce.com/" + reader.Value;
                            }
                            nextValueIsMapId = false;
                        }
                        if (reader.Value.ToString() == "mapid") {
                            nextValueIsMapId = true;
                        }
                    }
                }
            } else {
                int n;
                bool isNumeric = int.TryParse(cmrid, out n);

                if (isNumeric) {
                    res += ircchannel + " CMR #" + cmrid + " doesn't exist.";
                } else {
                    res += ircchannel + " Invalid CMR number.";
                }
            }
            return res;
        }



        private void RecordResultsJson(DataTable maps, int cmrid) {
            DataView dv = racers.DefaultView;
            dv.Sort = "Status, Hour, Min, Sec, TSec, Name";
            racers = dv.ToTable();

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw)) {
                writer.Formatting = Newtonsoft.Json.Formatting.Indented;

                writer.WriteStartObject();

                // CMR ID
                writer.WritePropertyName("cmr");
                writer.WriteValue(cmrid);

                #region Array of Maps
                writer.WritePropertyName("maps");
                writer.WriteStartArray();
                foreach (DataRow dr in maps.Rows) // Each row is one map object in Array of Maps
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("mapid");
                    writer.WriteValue(dr["mapid"].ToString());

                    writer.WritePropertyName("mapper");
                    writer.WriteValue(dr["mapper"].ToString());

                    writer.WritePropertyName("mapname");
                    writer.WriteValue(dr["mapname"].ToString());

                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                #endregion

                #region Array of Results
                writer.WritePropertyName("results");
                writer.WriteStartArray();

                int rank = 1;

                foreach (DataRow dr in racers.Rows) // Each row is one result object in Array of Results
                {
                    writer.WriteStartObject();

                    if (dr["Status"].ToString() == "1") {
                        writer.WritePropertyName("rank");
                        writer.WriteValue(rank);
                        rank++;
                    }

                    writer.WritePropertyName("name");
                    writer.WriteValue(dr["Name"].ToString());

                    writer.WritePropertyName("status");
                    writer.WriteValue(dr["Status"].ToString());

                    writer.WritePropertyName("time");
                    int hour = Convert.ToInt32(dr["Hour"]);
                    int min = Convert.ToInt32(dr["Min"]);
                    int sec = Convert.ToInt32(dr["Sec"]);
                    if (dr["Status"].ToString() == "1") {
                        TimeSpan time = new TimeSpan(hour, min, sec);
                        writer.WriteValue(time.ToString(@"%h\:mm\:ss"));
                    } else {
                        writer.WriteValue("");
                    }

                    writer.WritePropertyName("comment");
                    writer.WriteValue(dr["Comment"].ToString());

                    writer.WritePropertyName("rating");
                    writer.WriteValue(dr["rating"].ToString());

                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                #endregion

                writer.WriteEndObject();
            }

            System.IO.File.WriteAllText(DATA_PATH + @"CMR Results\CMR" + cmrid + @"Results.json", sb.ToString()); // !! FILEPATH !!
        }



        private void RecordResultsReddit(int cmrid) {
            DataView dv = racers.DefaultView;
            dv.Sort = "Status, Hour, Min, Sec, TSec, Name";
            racers = dv.ToTable();

            string[] lines = new string[racers.Rows.Count + 2];

            lines[0] = @"|Rank|Name|Time|Comment|Rating";
            lines[1] = @"|:-|:-|:-|:-|:-|";

            for (int i = 0; i < racers.Rows.Count; i++) {
                string name = racers.Rows[i]["Name"].ToString();
                int status = Convert.ToInt32(racers.Rows[i]["Status"]);
                string rank = GetRanking(name);
                int hour = Convert.ToInt32(racers.Rows[i]["Hour"]);
                int min = Convert.ToInt32(racers.Rows[i]["Min"]);
                int sec = Convert.ToInt32(racers.Rows[i]["Sec"]);
                TimeSpan time = new TimeSpan(hour, min, sec);
                string comment = racers.Rows[i]["Comment"].ToString();
                string rating = racers.Rows[i]["Rating"].ToString();

                lines[i + 2] = "|" + rank + "|" + name + "|" + time.ToString(@"%h\:mm\:ss") + "|" + comment + "|" + rating + "|";
            }

            System.IO.File.WriteAllLines(DATA_PATH + @"CMR Results\Reddit" + cmrid + @".txt", lines); // !! FILEPATH !!
        }



        static string ReadApiLeaderboardToJson(string mapname, int mapid, int page) {
            using (var w = new WebClient()) {
                var json_data = string.Empty;

                try {
                    string realname = mapname.Replace(" ", "-");
                    int realpage = page * 10;
                    json_data = w.DownloadString(@"http://" + @"df.hitboxteam.com/backend6/scores.php?level=" + realname + @"-" + mapid + @"&offset=" + realpage + @"&max=10"); // !! FILEPATH !!
                } catch (Exception) { }

                return json_data;
            }
        }



        static DataTable ReadApiLeaderboardToDt(string json) {
            DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);

            DataTable dt = ds.Tables["scorelist"];

            return dt;
        }



        static string CheckSSTest(string user) {
            string res = "";
            int page = 0;

            while (res == "") {
                string json = ReadApiLeaderboardToJson("Matrixity", 3282, page);
                Console.WriteLine("JSON requested, page: " + page);

                JObject rss = JObject.Parse(json);

                if (rss["best_scores"].ToString() != "[]") {
                    var query =
                        from p in rss["best_scores"]
                        where (string)p["name"] == user
                        select new {
                            finesse = (string)p["score_finesse"],
                            thoroughness = (string)p["score_thoroughness"]
                        };

                    foreach (var item in query) {
                        res = item.finesse + item.thoroughness;
                    }

                    if (res == "") {
                        page++;
                    }
                } else {
                    res = "User not found.";
                }
            }
            return res;
        }


        //TODO FIX THIS SHIT
        //static bool CheckSS(string racer, string cmrid) {
        //    DataTable maps;
        //    maps = UpdateJsonToDtMaps(cmrid).Copy();

        //    bool res = false;

        //    int mapsCount = maps.Rows.Count;

        //    string[] score = new string[mapsCount - 1];

        //    for (int i = 0; i < mapsCount; i++) {
        //        DataTable scores;
        //        scores = ReadApiLeaderboardToDt(ReadApiLeaderboardToJson(maps.Rows[i]["mapname"].ToString(), Convert.ToInt32(maps.Rows[i]["mapid"]), 0)).Copy();
        //    }

        //    return res;
        //}



        static int DaysToAdd(DayOfWeek current, DayOfWeek desired) {
            int c = (int)current;
            int d = (int)desired;
            int n = (7 - c + d);

            return (n > 7) ? n % 7 : n;
        }



        static DateTime GetNextDateForDay(DateTime startDate, DayOfWeek desiredDay) {
            return startDate.AddDays(DaysToAdd(startDate.DayOfWeek, desiredDay));
        }



        static void Simulation() {
            DataTable dt = new DataTable();

            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Hour", typeof(int));
            dt.Columns.Add("Min", typeof(int));
            dt.Columns.Add("Sec", typeof(int));
            dt.Columns.Add("OldRating", typeof(decimal));
            dt.Columns.Add("NewRating", typeof(decimal));

            dt.Rows.Add("Tropicallo", 0, 10, 5, 38, 0);
            dt.Rows.Add("Itay", 0, 11, 6, 32, 0);
            dt.Rows.Add("Bird", 0, 13, 30, 41, 0);
            dt.Rows.Add("Calistus", 0, 15, 55, 24, 0);
            dt.Rows.Add("Furkie", 0, 18, 45, 24, 0);
            dt.Rows.Add("Krankdud", 0, 23, 30, 22, 0);
            dt.Rows.Add("Lawatson", 0, 26, 30, 0, 0);
            dt.Rows.Add("Virgate", 0, 29, 30, 15, 0);
            dt.Rows.Add("Ravencoff", 0, 34, 40, 12, 0);
            dt.Rows.Add("Lightningy", 0, 45, 30, 0, 0); // Please, anybody but Marksel!

            decimal maxRating = 50M;
            decimal range = maxRating / (decimal)dt.Rows.Count;
            decimal expectedRating;
            decimal avgRating;
            decimal expectedRangeMin;
            decimal expectedRangeMax;
            decimal birdInt = 2M;

            for (int i = 0; i < dt.Rows.Count; i++) {
                decimal j = (decimal)i;
                decimal currentPlayerRating = (decimal)dt.Rows[i]["OldRating"];
                expectedRating = maxRating - ((j + 1) * range);
                avgRating = (range / birdInt) + expectedRating;
                expectedRangeMin = expectedRating;
                expectedRangeMax = maxRating - (j * range);

                if (expectedRating > currentPlayerRating) {
                    dt.Rows[i]["NewRating"] = currentPlayerRating + ((avgRating - currentPlayerRating) / birdInt);
                } else {
                    dt.Rows[i]["NewRating"] = currentPlayerRating - (currentPlayerRating - avgRating);
                }

                for (int comparedPlayer = 0; i < comparedPlayer; comparedPlayer++) {
                    decimal comparedPlayerRating = (decimal)dt.Rows[comparedPlayer]["OldRating"];
                    if (currentPlayerRating > comparedPlayerRating) {
                        dt.Rows[comparedPlayer]["NewRating"] = comparedPlayerRating + ((currentPlayerRating - comparedPlayerRating) / birdInt);
                        int comparedPlayerTime = (Convert.ToInt32(dt.Rows[comparedPlayer]["Hour"]) * 60) + (Convert.ToInt32(dt.Rows[comparedPlayer]["Min"]) * 60) + (Convert.ToInt32(dt.Rows[comparedPlayer]["Sec"]));
                        int currentPlayerTime = (Convert.ToInt32(dt.Rows[i]["Hour"]) * 60) + (Convert.ToInt32(dt.Rows[i]["Min"]) * 60) + (Convert.ToInt32(dt.Rows[i]["Sec"]));
                        int timeDifference = currentPlayerTime - comparedPlayerTime;
                        dt.Rows[comparedPlayer]["NewRating"] = comparedPlayerRating + ((decimal)timeDifference / 100);
                    }
                    if (currentPlayerRating == comparedPlayerRating) {
                        int comparedPlayerTime = (Convert.ToInt32(dt.Rows[comparedPlayer]["Hour"]) * 60) + (Convert.ToInt32(dt.Rows[comparedPlayer]["Min"]) * 60) + (Convert.ToInt32(dt.Rows[comparedPlayer]["Sec"]));
                        int currentPlayerTime = (Convert.ToInt32(dt.Rows[i]["Hour"]) * 60) + (Convert.ToInt32(dt.Rows[i]["Min"]) * 60) + (Convert.ToInt32(dt.Rows[i]["Sec"]));
                        int timeDifference = currentPlayerTime - comparedPlayerTime;
                        dt.Rows[i]["NewRating"] = currentPlayerRating - ((decimal)timeDifference / 100);
                    }
                }

                Console.WriteLine((decimal)dt.Rows[i]["OldRating"] + "\t" + (decimal)dt.Rows[i]["NewRating"] + "\t" + ((decimal)dt.Rows[i]["NewRating"] - (decimal)dt.Rows[i]["OldRating"]));
            }

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





        /// <summary>
        /// Gets the irc password for the bot (so that its not stored publicly in the code).
        /// </summary>
        /// <returns>the IRC pass loaded from the default IRC password file location</returns>
        public static string GetIRCPass() {
            string toReturn = "";
            if (File.Exists(PASS_PATH)) {
                Console.WriteLine("Trying to load map file.");
                string[] passArray = File.ReadAllLines(PASS_PATH);
                if (passArray[0].Trim() != "") {
                    toReturn = passArray[0].Trim();
                } else {
                    Console.WriteLine(@"Password wasnt at the top line of the Password file. Default file is " + PASS_PATH);
                    Console.ReadLine();
                    Environment.Exit(1);
                }
            } else {
                Console.WriteLine(@"Password wasnt at the top line of the Password file. Default file is " + PASS_PATH);
                Console.ReadLine();
                Environment.Exit(1);
            }
            return toReturn;
        } 


    } /* IRCBot */



    internal class Program {
        private static void Main(string[] args) {

            IRCConfig conf = new IRCConfig();
            conf.name = FurkieBot.BOT_NAME;
            conf.nick = FurkieBot.BOT_NAME;
            conf.altNick = "FurkieBot_";
            conf.port = 6667;
            conf.server = "irc2.speedrunslive.com";
            conf.pass = FurkieBot.GetIRCPass();
            using (var bot = new FurkieBot(conf)) {
                bot.Connect();
                bot.IRCWork();
            }
            Console.WriteLine("Furkiebot quit/crashed");
            Console.ReadLine();

        } /* Main() */




    } /* Program */
}
