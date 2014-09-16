/**
 * FurkieBot
 * Program.cs
 * @author FurkiePurkie
 */


/*
 * IRC CODES https://www.alien.net.au/irc/irc2numerics.html
 */

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
//using System.Data.OleDb;
//using DocumentFormat.OpenXml;
using ClosedXML.Excel;
//using System.IO.Packaging; //WindowsBase.dll
using System.Net;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace FurkiebotCMR {

    internal struct IRCConfig {
        public string server;
        public int port;
        public string nick;
        public string name;
        public string pass;
        public string altNick;
    } /* IRCConfig */


    internal struct PlayerInfo {
        public string ircname;
        public string dustforcename;
        public string streamurl;
        public bool tester;
        public bool trusted;
        public bool admin;
        public int rating;
        public int randmaprating;
        public string password;
        public string salt;
    }

    internal class FurkieBot : IDisposable {
        public static string SEP = ColourChanger(" | ", "07"); //The orange | seperator also used by GLaDOS
        public const string MAPS_PATH = @"C:\CMRmaps";
        public const int MIN_MAPS = 6;
         

        private TcpClient IRCConnection = null;
        private IRCConfig config;
        private NetworkStream ns = null;
        private BufferedStream bs = null;
        private StreamReader sr = null;
        private StreamWriter sw = null;

        private FileSystemWatcher pendingWatcher;   //watches the pending maps folder.
        private FileSystemWatcher acceptedWatcher;  //watches the accepted maps folder.


        private DataTable racers;
        private DataTable users;
        private HashSet<string> acceptedMaps;
        private HashSet<string> pendingMaps;
        private Dictionary<string, PlayerInfo> userlist; //ircnames -> userinfo. used for quick lookup and serializing to userlistmap.json upon modification
        private Dictionary<string, PlayerInfo> dustforcelist;// dustforcenames -> userinfo. used only for quick lookup, and duplicate dustforcename checking.
        private Dictionary<string, bool> identlist;// dustforcenames -> userinfo. used only for quick lookup, and duplicate dustforcename checking.


        private string dummyRacingChan; //first part of racingchannel string
        private string realRacingChan; //real racing channel string
        private string mainchannel; //also the channel that will be joined upon start, change to #dustforcee for testing purposes
        private string cmrchannel; //also the channel that will be joined upon start, change to #dustforcee for testing purposes
        private string cmrId;
        private string comNames; // Used for NAMES commands
        private bool complexAllowed; //Set to false when a function is already waiting on a complex return, ie IsIdentified while parsing a /whois. Keeps additional complex functions from starting in the meantime.

        private TimeSpan cmrtime;//At what time (local) it is possible to start a CMR, 8:30pm equals 6:30pm GMT for me  EDIT now 10:30 AM PST for 6:30 GMT
        private string cmrtimeString; //make sure this equals the time on TimeSpan cmrtime

        private string cmrStatus;

        bool hype; //Just for .unhype command lol

        Stopwatch stahpwatch; //Timer used for races
        Stopwatch countdown; //Timer used to countdown a race



        //prints a string to console.
        public static void p(string toPrint) {
            Console.WriteLine(toPrint);
        }


        


        /**
         * Constructor for FurkieBot, I'm guessing.
         */
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


            loadUserlist();

            dummyRacingChan = "#cmr-"; //first part of racingchannel string
            realRacingChan = ""; //real racing channel string
            mainchannel = "#dustforcee"; //also the channel that will be joined upon start, change to #dustforcee for testing purposes
            cmrchannel = "#DFcmr";
            cmrId = GetCurrentCMRID();
            comNames = ""; // Used for NAMES commands

            complexAllowed = true;


            cmrStatus = GetCurrentCMRStatus(); //CMR status can be closed, open, racing or finished
            identlist = new Dictionary<string, bool>();

            hype = true; //Just for .unhype command lol

            stahpwatch = new Stopwatch(); //Timer used for races
            countdown = new Stopwatch(); //Timer used to countdown a race


            cmrtime = new TimeSpan(10, 30, 0); //At what time (local) it is possible to start a CMR, 8:30pm equals 6:30pm GMT for me  EDIT now 10:30 AM PST for 6:30 GMT
            cmrtimeString = @"10:30:00"; //make sure this equals the time on TimeSpan cmrtime


            pendingMaps = new HashSet<String>(Directory.GetFiles("C:\\CMRmaps\\" + cmrId + "\\pending", "*").Select(path => Path.GetFileName(path)).ToArray());
            acceptedMaps = new HashSet<String>(Directory.GetFiles("C:\\CMRmaps\\" + cmrId + "\\accepted", "*").Select(path => Path.GetFileName(path)).ToArray());


            /*
             * Set up the event handlers for watching the CMR map filesystem. Solution for now. 
             */
            pendingWatcher = new FileSystemWatcher();
            acceptedWatcher = new FileSystemWatcher();
            pendingWatcher.Path = MAPS_PATH + "\\" + cmrId + "\\pending";
            acceptedWatcher.Path = MAPS_PATH + "\\" + cmrId + "\\accepted";
            /* Watch for changes in LastWrite times, and
               the renaming of files or directories. */
            pendingWatcher.NotifyFilter = NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            acceptedWatcher.NotifyFilter = NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch text files.
            //fileWatcher.Filter = "*.txt";

            // Add event handlers.
            //pendingWatcher.Changed += new FileSystemEventHandler(OnChanged);
            pendingWatcher.Created += new FileSystemEventHandler(CreatedPending);
            pendingWatcher.Deleted += new FileSystemEventHandler(DeletedPending);
            //pendingWatcher.Renamed += new RenamedEventHandler(OnRenamed);

            //acceptedWatcher.Changed += new FileSystemEventHandler(OnChanged);
            acceptedWatcher.Created += new FileSystemEventHandler(CreatedAccepted);
            acceptedWatcher.Deleted += new FileSystemEventHandler(DeletedAccepted);
            //acceptedWatcher.Renamed += new RenamedEventHandler(OnRenamed);

            // Begin watching.
            pendingWatcher.EnableRaisingEvents = true;
            acceptedWatcher.EnableRaisingEvents = true;

        }




        // Define the filesystem event handlers. 
        private void CreatedPending(object source, FileSystemEventArgs e) {
            // Specify what is done when a file is changed, created, or deleted.
            //Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            Console.WriteLine("\nCreatedPending: " + e.FullPath + " " + e.ChangeType + "\n");
            string fileName = Path.GetFileName(e.FullPath);
            pendingMaps.Add(fileName);
            string toSay = " :New map submitted for testing: \"";

            string[] split = fileName.Split('-');
            for (int i = 1; i < split.Length; i++) {
                toSay += split[i];
            }
            toSay += "\" by " + split[0];

            sendData("PRIVMSG", mainchannel + toSay);
            sendData("PRIVMSG", cmrchannel + toSay);
        }



        // Define the filesystem event handlers. 
        private void DeletedPending(object source, FileSystemEventArgs e) {
            // Specify what is done when a file is changed, created, or deleted.
            //Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            Console.WriteLine("\nDeletedPending: " + e.FullPath + " " + e.ChangeType + "\n");
            string fileName = Path.GetFileName(e.FullPath);
            pendingMaps.Remove(fileName);
        }



        private void CreatedAccepted(object source, FileSystemEventArgs e) {
            // Specify what is done when a file is renamed.
            //Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
            Console.WriteLine("\nCreatedAccepted: " + e.FullPath + " " + e.ChangeType + "\n");
            string fileName = Path.GetFileName(e.FullPath);
            acceptedMaps.Add(fileName);
            pendingMaps.Remove(fileName);

            string toSay = " :Map accepted: \"";

            string[] split = fileName.Split('-');
            for (int i = 1; i < split.Length; i++) {
                toSay += split[i];
            }
            toSay += "\" by " + split[0];

            sendData("PRIVMSG", mainchannel + toSay);
            sendData("PRIVMSG", cmrchannel + toSay);
        }




        // Define the filesystem event handlers. 
        private void DeletedAccepted(object source, FileSystemEventArgs e) {
            // Specify what is done when a file is changed, created, or deleted.
            //Console.WriteLine("File: " + e.FullPath + " " + e.ChangeType);
            Console.WriteLine("\nDeletedAccepted: " + e.FullPath + " " + e.ChangeType + "\n");
            string fileName = Path.GetFileName(e.FullPath);
            acceptedMaps.Remove(fileName);

            string toSay = " :Map un accepted: \"";

            string[] split = fileName.Split('-');
            for (int i = 1; i < split.Length; i++) {
                toSay += split[i];
            }
            toSay += "\" by " + split[0];

            sendData("PRIVMSG", mainchannel + toSay);
            sendData("PRIVMSG", cmrchannel + toSay);
        }

        //private static void OnRenamed(object source, RenamedEventArgs e) {
        //    // Specify what is done when a file is renamed.
        //    //Console.WriteLine("File: {0} renamed to {1}", e.OldFullPath, e.FullPath);
        //    Console.WriteLine("\n\n\nFile: {0} renamed to {1}\n\n\n", e.OldFullPath, e.FullPath);
        //}




        private void OutputMapStatus(string chan) {
            OutputPending(chan);
            OutputAccepted(chan);
        }



        
        private void OutputPending(string chan) {
            string toSay = " :" + pendingMaps.Count + " Pending testing ";

            foreach (string s in pendingMaps) {
                toSay += SEP + "\"";
                string[] split = s.Split('-');
                for (int i = 1; i < split.Length; i++) {
                    toSay += split[i];
                }
                toSay += "\" by " + split[0];
            }

            if (chan == null || chan == "" || chan == " ") {
                sendData("PRIVMSG", mainchannel + toSay);
                sendData("PRIVMSG", cmrchannel + toSay);
            } else {
                sendData("PRIVMSG", chan + toSay);
            }
        }



        private void OutputAccepted(string chan) {
            string toSay = " :" + acceptedMaps.Count + " Accepted ";
            foreach (string s in acceptedMaps) {
                toSay += SEP + "\"";
                string[] split = s.Split('-');
                for (int i = 1; i < split.Length; i++) {
                    toSay += split[i];
                }
                toSay += "\" by " + split[0];
            }

            if (chan == null || chan == "" || chan == " ") {
                sendData("PRIVMSG", mainchannel + toSay);
                sendData("PRIVMSG", cmrchannel + toSay);
            } else {
                sendData("PRIVMSG", chan + toSay);
            }
        }




        private void loadUserlist() {
            string filepath = @"..\..\..\Data\Userlist\userlistmap.json"; // !! FILEPATH !!
            string[] jsonarray = File.ReadAllLines(filepath);
            string json = string.Join("", jsonarray);
            userlist = JsonConvert.DeserializeObject<Dictionary<string, PlayerInfo>>(json); // initially loads the userlist from JSON
            dustforcelist = new Dictionary<string, PlayerInfo>();
            foreach (KeyValuePair<string, PlayerInfo> entry in userlist) {
                dustforcelist.Add(entry.Value.dustforcename, entry.Value);
                Console.WriteLine("name " + entry.Value.ircname + " dustforcename " + entry.Value.dustforcename + " tester " + entry.Value.tester + " trusted " + entry.Value.trusted + " admin " + entry.Value.admin);
            }
        } /* IRCBot */



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



        public void sendData(string cmd, string param) {
            if (param == null) {
                sw.WriteLine(cmd);
                sw.Flush();
                Console.WriteLine(cmd);
            } else {
                if (param.Length > 400) //Makes sure to send multiple messages in case a message is too long for irc
                {
                    string channel = "";
                    if (param[0] == '#') {
                        channel = param.Substring(0, param.IndexOf(" ")) + " ";

                        param = param.Remove(0, channel.Length);
                    }

                    string ss = param;
                    int size = ss.Length / 350;
                    string[] newParam = new string[size + 1];

                    for (int i = 0; i < size + 1; i++) {
                        newParam[i] = ss.Substring(0, Math.Min(ss.Length, 350));

                        if (i != size)
                            ss = ss.Remove(0, 350);

                        if (i != 0) {
                            string lastword = newParam[i - 1].Substring(newParam[i - 1].LastIndexOf(' ') + 1);
                            string firstword = newParam[i].Substring(0, newParam[i].IndexOf(" "));

                            if (lastword != "" && firstword != "") {
                                newParam[i] = newParam[i].Insert(0, lastword);
                                newParam[i - 1] = newParam[i - 1].Remove(350 - (lastword.Length + 1), lastword.Length + 1);
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
        }  /* sendData() */



        /**
         * Checks if a nickname is registered. If not, notifies the user that they must register.
         */
        private bool IsRegistered(string nick) {
            if (userlist.ContainsKey(nick)) {
                if (userlist[nick].password != "") {
                    return true;
                } else {
                    sendData("NOTICE", nick + " :You'll need to register your nick with FurkieBot before you may do this. Type .help register for more info.");
                    return false;
                }
            } else {
                sendData("NOTICE", nick + " :You'll need to register your nick with FurkieBot before you may do this. Type .help register for more info.");
                return false;
            }
        }



        /**
         * Checks if a nickname is an admin.
         */
        private bool IsAdmin(string nick) {
            nick = nick.ToLower();
            if (IsRegistered(nick) && IsIdentified(nick)) {
                return userlist[nick].admin;
            } else return false;
        }



        /**
         * Checks to see if a user is identified.
         */
        private bool IsIdentified(string nick) {
            if (identlist.ContainsKey(nick) && identlist[nick]) {
                return true;
            } else {

                complexAllowed = false;
                sendData("WHOIS", nick);

                bool isIdentified = false;
                bool is318 = false;

                while (!is318) {
                    Console.WriteLine(" ");
                    Console.WriteLine("Waiting on whois for " + nick);

                    string[] ex;
                    string data;

                    data = sr.ReadLine();
                    Console.WriteLine(data);
                    char[] charSeparator = new char[] { ' ' };
                    ex = data.Split(charSeparator, 5);
                    if (ex[2] == "307") {
                        isIdentified = true;
                        Console.WriteLine("Successfully identified " + nick);
                    } else if (ex[2] == "318") {
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
        }







        /**
         * Main loop for the bot.
         */
        public void IRCWork() {
            bool shouldRun = true;

            while (shouldRun) {
                Console.WriteLine(" ");

                string[] ex;
                string data;

                data = sr.ReadLine();
                Console.WriteLine(data);
                char[] charSeparator = new char[] { ' ' };
                ex = data.Split(charSeparator, 5);

                shouldRun = ProcessInput(ex, data, charSeparator);
                
                //Console.WriteLine("End Last Switch " + parseTimer.Elapsed);
            }
        }






        /**
         * The code that processes incoming lines from the IRC Server.
         */
        private bool ProcessInput(string[] ex, string data, char[] charSeparator) {
            bool shouldRun = true;

            //Just some Regex bullshit to get username from full name/hostname shit
            string inputt = ex[0];
            string re22 = "((?:[a-z][a-z0-9_]*))";
            Regex rr = new Regex(re22, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match usernamee = rr.Match(inputt);
            string nickname = usernamee.ToString();

            #region FurkieBot Output String list
            string[] fbOutput = new string[8];
            fbOutput[0] = " Your Dustforce name is currently registered as " + ColourChanger(getUserIgn(nickname), "03") + ". If your name has changed, please set a new nickname using " + BoldText(".setign dustforcename");
            fbOutput[1] = " You need to register your Dustforce name in order to join a race. Type " + BoldText(".setign dustforcename") + " to register the name you use in Dustforce";
            //.furkiebot #dustforce
            //.furkiebot #cmr-xxxxx
            #endregion






            if (ex[0] == "PING") //Pinging server in order to stay connected
                {
                sendData("PONG", ex[1]);
            }





            switch (ex[1]) //Events
            {
                case "001": //Autojoin channel when first response from server
                    sendData("JOIN", mainchannel);
                    sendData("PRIVMSG", "NickServ" + " ghost " + config.nick + " " + config.pass);
                    sendData("JOIN", cmrchannel);

                    //OutputMapStatus(null);
                    break;
                case "433": //Changes nickname to altNick when nickname is already taken
                    sendData("NICK", config.altNick);
                    break;
                case "353": //NAMES command answer from server
                    if (comNames == "CANCEL") //Kick all irc users from channel
                        {
                        int amount = CountCertainCharacters(data, ' ') - 5;
                        string r = ex[4].Substring(ex[4].IndexOf(@":") + 1);
                        string[] name = r.Split(charSeparator, amount);
                        foreach (string s in name) {
                            char[] gottaTrimIt = new char[] { '@', '+', '%' };
                            string n = s.Trim().TrimStart(gottaTrimIt);
                            if (n != "FurkieBot") {
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
                    //DISABLED DUE TO PERMORMANCE ISSUES
                    if (ex[2] == ":" + realRacingChan) {
                        if (StringCompareNoCaps(getUserIrc(nickname), nickname)) {
                            sendData("NOTICE", nickname + fbOutput[0]);
                        } else {
                            sendData("NOTICE", nickname + fbOutput[1]);
                        }
                        if (CheckEntrant(racers, nickname)) {
                            sendData("MODE", realRacingChan + " +v " + nickname);
                        }
                    }

                    if (ex[2] == ":" + mainchannel) //Event: When someone joins main channel
                        {
                        if (cmrStatus == "open") //Message sent to someone that joins the main channel, notifying that there's a CMR open at the moment
                            {
                            sendData("NOTICE", nickname + " Entry currently " + ColourChanger("OPEN", "03") + " for Custom Map Race " + cmrId + ". Join the CMR at " + ColourChanger(realRacingChan, "04") + ". " + CountEntrants(racers) + " entrants");
                        }
                        if (cmrStatus == "racing") //Message sent to someone that joins the main channel, notifying that there's a CMR going on at the moment
                            {
                            sendData("NOTICE", nickname + " Custom Map Race " + cmrId + " is currently " + ColourChanger("In Progress", "12") + " at " + ColourChanger(realRacingChan, "04") + ". " + CountEntrants(racers) + " entrants");
                        }
                    }
                    break;
                    #endregion
                //default:
                //    break;
            }



            //Console.WriteLine("End event switch " + parseTimer.Elapsed);













            if (ex.Length == 4) //Commands without parameters
                {
                string command = ex[3]; //grab the command sent

                switch (command) {
                    case ":.furkiebot": //FurkieBot Commands
                        if (!StringCompareNoCaps(ex[2], realRacingChan)) //FurkieBot commands for the main channel
                            {

                            sendData("PRIVMSG", ex[2] + " Commands: .cmr" + SEP + ".maps" + SEP + ".startcmr" + SEP + ".ign <ircname>" + SEP + ".setign <in-game name>" + SEP + ".mappack" + SEP + ".pending" + SEP + ".accepted");
                            sendData("PRIVMSG", ex[2] + @" CMR info: http://eklipz.us.to/cmr" + SEP + @"Command list: https://github.com/EklipZgit/furkiebot/wiki" + SEP + "FurkieBot announce channel: #DFcmr");

                        } else {            // FurkieBot commands for race channel
                            sendData("PRIVMSG", ex[2] + @" Command list: https://github.com/EklipZgit/furkiebot/wiki");
                            //sendData("PRIVMSG", ex[2] + " Commands: .entrants" + SEP + ".join" + SEP + ".unjoin" + SEP + ".ready" + SEP + ".unready" + SEP + ".done" + SEP + ".undone" + SEP + ".forfeit" + SEP + ".ign <ircname>" + SEP + ".setign <in-game name>");                    
                        }
                        break;

                    case ":.help":
                        goto case ":.furkiebot";

                    case ":.commands":
                        goto case ":.furkiebot";

                    case ":.commandlist":
                        goto case ":.furkiebot";

                    case ":.cmr": //General CMR FAQ
                        #region
                        //goto case ":.cmrmaps";
                        OutputCMRinfo(ex[2], cmrtime, cmrtimeString, nickname);
                        break;
                        #endregion

                    case ":.startcmr+": // Used for testing purposes, forces the start of a race without having to worry about the date and time, make sure to use this command when mainchannel is NOT #dustforce
                        if (IsAdmin(nickname)) {
                            cmrtime = DateTime.Now.TimeOfDay;
                        }
                        goto case ":.startcmr";

                    case ":.startcmr": //Opening that CMR hype
                        #region
                        if (cmrStatus == "closed") {
                            // Veryfying whether it is Saturday and if the time matches with CMR time
                            DateTime saturday;
                            if (DateTime.Now.DayOfWeek != DayOfWeek.Saturday) {
                                if (cmrtime.ToString(@"%h\:mm\:ss") == cmrtimeString) {
                                    saturday = GetNextDateForDay(DateTime.Now, DayOfWeek.Saturday).Date;
                                } else

                                    saturday = DateTime.Now.Date;
                            } else {
                                saturday = DateTime.Now.Date;
                            }
                            DateTime cmrday = saturday.Date + cmrtime;
                            TimeSpan duration = cmrday - DateTime.Now;
                            string nextCmrD = duration.Days.ToString();
                            string nextCmrH = duration.Hours.ToString();
                            string nextCmrM = duration.Minutes.ToString();
                            string nextCmrS = duration.Seconds.ToString();

                            if (CmrMapCount(cmrId) < 6 && cmrtime.ToString(@"%h\:mm\:ss") == "10:30:00") // If there are less than 6 maps submitted AND if command wasn't issued using .startcmr+
                                {
                                sendData("PRIVMSG", ex[2] + " " + "There are not enough maps to start a CMR. We need " + (6 - CmrMapCount(cmrId)).ToString() + " more maps to start a CMR.");
                            } else {
                                TimeSpan stopTheTime = new TimeSpan(20, 29, 20);
                                DateTime stopTheSpam = saturday.Date + stopTheTime;
                                if (DateTime.Now < cmrday && DateTime.Now > stopTheSpam) {
                                    sendData("PRIVMSG", ex[2] + " " + "I get it, I can start a racechannel very soon. Jeez, stop spamming already (??;)");
                                }
                                if (DateTime.Now < cmrday && DateTime.Now < stopTheSpam) {
                                    sendData("PRIVMSG", ex[2] + " " + "We have enough maps to start Custom Map Race " + cmrId + ", race can be initiated in "
                                        + ColourChanger(nextCmrD + " days, "
                                        + nextCmrH + " hours, "
                                        + nextCmrM + " minutes and "
                                        + nextCmrS + " seconds", "03") + ".");
                                }
                                if (DateTime.Now > cmrday) {
                                    if (StringCompareNoCaps(nickname, "furkiepurkie") || StringCompareNoCaps(nickname, "furkiemobile") || StringCompareNoCaps(nickname, "eklipz")) {    // TODO REPLACE WITH ISADMIN
                                        realRacingChan = "";
                                        dummyRacingChan += RandomCharGenerator(5, 1);
                                        realRacingChan = dummyRacingChan;
                                        sendData("JOIN", realRacingChan);
                                        sendData("PRIVMSG", "TRAXBUSTER" + " .join001 " + realRacingChan);
                                        cmrStatus = "open";
                                        sendData("PRIVMSG", ex[2] + " " + "Race initiated for Custom Map Race " + cmrId + ". Join " + ColourChanger(realRacingChan, "04") + " to participate.");
                                        sendData("TOPIC", realRacingChan + " " + ":Status: Entry Open | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                        sendData("MODE", realRacingChan + " +t");
                                    } else {
                                        sendData("PRIVMSG", ex[2] + " Only Furkiepurkie can start the race for now, please get him instead.");
                                    }
                                }
                            }
                        } else {
                            sendData("PRIVMSG", ex[2] + " " + "Custom Map Race " + cmrId + " has already been iniatied. Join " + realRacingChan + " to participate.");
                        }
                        break;
                        #endregion

                    case ":.cancelcmr": //Shattering everyones dreams by destroying that CMR hype
                        #region
                        if (StringCompareNoCaps(nickname, "furkiepurkie") || StringCompareNoCaps(nickname, "eklipz")) {  // TODO REPLACE WITH ISADMIN
                            if (cmrStatus == "open" || cmrStatus == "finished" || cmrStatus == "racing") {
                                if (ex[2] == realRacingChan) {
                                    cmrStatus = "closed";
                                    sendData("PRIVMSG", ex[2] + " " + "Custom Map Race " + cmrId + " has been cancelled by " + nickname + ".");
                                    sendData("PRIVMSG", mainchannel + " " + "Custom Map Race " + cmrId + " has been cancelled.");
                                    sendData("TOPIC", realRacingChan + " " + ":Status: Cancelled | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                    //for (int i = 0; i < CountEntrants(racers); i++)
                                    //{
                                    //    string name2DeVoice = racers.Rows[i]["Name"].ToString();
                                    //    sendData("MODE", realRacingChan + " -v " + name2DeVoice);
                                    //}
                                    comNames = "CANCEL";
                                    sendData("NAMES", realRacingChan);
                                    sendData("MODE", realRacingChan + " +im");
                                    racers.Clear();
                                } else {
                                    sendData("PRIVMSG", ex[2] + " " + "A race can only be cancelled in the CMR racing channel " + realRacingChan);
                                }
                            }
                        }
                        break;
                        #endregion

                    case ":.closecmr":
                        #region
                        if (StringCompareNoCaps(nickname, "furkiepurkie") || StringCompareNoCaps(nickname, "eklipz")) {  // TODO REPLACE WITH ISADMIN
                            if (cmrStatus == "open" || cmrStatus == "finished" || cmrStatus == "racing") {
                                if (ex[2] == realRacingChan) {
                                    if (cmrStatus == "finished") {
                                        cmrStatus = "closed";
                                        sendData("PRIVMSG", ex[2] + " " + "Custom Map Race " + cmrId + " has been closed by " + nickname + ".");
                                        sendData("TOPIC", realRacingChan + " " + ":Status: Closed | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                        for (int i = 0; i < CountEntrants(racers); i++) {
                                            string name2DeVoice = racers.Rows[i]["Name"].ToString();
                                            sendData("MODE", realRacingChan + " -v " + name2DeVoice);
                                        }
                                        comNames = "CANCEL";
                                        sendData("NAMES", realRacingChan);
                                        sendData("MODE", realRacingChan + " +im");
                                        racers.Clear();
                                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"..\..\..\Data\CMR_ID.txt")) // !! FILEPATH !!
                                            {
                                            int newid = Convert.ToInt32(cmrId) + 1;
                                            file.WriteLine(newid.ToString());
                                        }
                                    }
                                }
                            }
                        }
                        break;
                        #endregion

                    case ":.maps": //Shows a list of currently approved maps
                        #region
                        goto case ":.cmrmaps";

                    case ":.cmrmaps":
                        OutputMapStatus(ex[2]);
                        break;

                    case ":.pending":
                        OutputPending(ex[2]);
                        break;

                    case ":.accepted":
                        OutputAccepted(ex[2]);
                        break;
                        #endregion

                    case ":.entrants": //Shows a list of the users currently in a race
                        #region
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            sendData("PRIVMSG", ex[2] + " " + GetEntrants(racers, stahpwatch));
                        }
                        #endregion
                        break;

                    case ":.join": //Get that fool in the CMR hype
                        #region
                        goto case ":.enter";
                    case ":.enter":
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            if (cmrStatus == "open") //Command only works if CMR is open
                                {
                                if (getUserIgn(nickname) != "+") {
                                    if (!CheckEntrant(racers, nickname)) //Command only works if user isn't part of the race
                                        {
                                        //Add user to race
                                        AddEntrant(nickname);
                                        string extraS = "";
                                        if (CountEntrants(racers) > 1) {
                                            extraS = "s";
                                        }
                                        sendData("PRIVMSG", ex[2] + " " + nickname + " (" + getUserIgn(nickname) + ") enters the race! " + CountEntrants(racers) + " entrant" + extraS + ".");
                                        sendData("MODE", realRacingChan + " +v " + nickname);
                                    } else {
                                        sendData("PRIVMSG", ex[2] + " " + nickname + " already entered the race.");
                                    }
                                } else {
                                    sendData("PRIVMSG", ex[2] + " No ign registered.");
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.unjoin": //Remove that fool from the CMR hype
                        #region
                        goto case ":.unenter";
                    case ":.unenter":
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            if (cmrStatus == "open") //Command only works if CMR is open
                                {
                                if (GetStatus(racers, nickname) == 6 || GetStatus(racers, nickname) == 3) //Command only works if racer status is "standby" or "ready"
                                    {
                                    //Remove user from race
                                    RemoveEntrant(racers, nickname);
                                    string extraS = "";
                                    if (CountEntrants(racers) > 1 || CountEntrants(racers) == 0) {
                                        extraS = "s";
                                    }
                                    sendData("PRIVMSG", ex[2] + " " + nickname + " has been removed from the race. " + CountEntrants(racers) + " entrant" + extraS + ".");
                                    sendData("MODE", realRacingChan + " -v " + nickname);
                                }
                                if (ComfirmMassStatus(racers, 3) && racers.Rows.Count > 1) {
                                    goto case ":.go";
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.ready": //Gotta get them ready for the upcoming CMR maps
                        #region
                        if (ex[2] == realRacingChan) //Command is only possible in racing channel
                            {
                            if (cmrStatus == "open") //Command is only possible if CMR is open
                                {
                                if (GetStatus(racers, nickname) == 6) //Comment only works if racer status is "standby"
                                    {
                                    //Set racer status to "ready"
                                    SetStatus(racers, nickname, 3);
                                    int notReadyCount = CountEntrants(racers) - CountStatus(racers, 3);
                                    sendData("PRIVMSG", ex[2] + " " + nickname + " is ready. " + notReadyCount + " remaining.");
                                }
                                if (ComfirmMassStatus(racers, 3) && racers.Rows.Count > 1) {
                                    goto case ":.go";
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.unready": //NO WAIT IM NOT READY YET
                        #region
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            if (cmrStatus == "open") //Command only works if CMR is open
                                {
                                if (GetStatus(racers, nickname) == 3) //Command only works if racer status is "ready"
                                    {
                                    //Set racer status to "standby"
                                    SetStatus(racers, nickname, 6);
                                    int notReadyCount = CountEntrants(racers) - CountStatus(racers, 3);
                                    sendData("PRIVMSG", ex[2] + " " + nickname + " is not ready. " + notReadyCount + " remaining.");
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.quit":
                        #region
                        goto case ":.forfeit";
                    case ":.forfeit":
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            if (cmrStatus == "racing") //Command only works if CMR is open
                                {
                                if (GetStatus(racers, nickname) == 2) //Command only works if racer status is "racing"
                                    {
                                    //Set racer status to "quit"
                                    SetStatus(racers, nickname, 4);
                                    sendData("PRIVMSG", ex[2] + " " + nickname + " has forfeited from the race.");
                                    if (ComfirmDoubleMassStatus(racers, 4, 5)) //Stop the race if all racers are "quit"/"dq"
                                        {
                                        StopRace(stahpwatch);
                                        cmrStatus = "finished";
                                        sendData("TOPIC", realRacingChan + " " + ":Status: Complete | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                        sendData("PRIVMSG", mainchannel + " " + "Race Finished: Dustforce - Custom Map Race " + cmrId + " | No one was able to finish the race. The race ended at " + GetTimeRank(racers, 1));
                                    } else {
                                        if (ComfirmTripleMassStatus(racers, 1, 4, 5)) //Stop the race if all racers are "done"/"quit"/"dq"
                                            {
                                            StopRace(stahpwatch);
                                            cmrStatus = "finished";
                                            sendData("TOPIC", realRacingChan + " " + ":Status: Complete | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                            sendData("PRIVMSG", mainchannel + " " + "Race Finished: Dustforce - Custom Map Race " + cmrId + " | " + GetNameRank(racers, 1) + " won with a time of " + GetTimeRank(racers, 1));
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.go": //Starts race and timer
                        #region
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            if (cmrStatus == "open") //Command only works if CMR is open
                                {
                                if (CountEntrants(racers) > 0) //Command only works if there is at least 1 racer
                                    {
                                    if (ComfirmMassStatus(racers, 3)) //Command only works if all racers have status on "ready"
                                        {
                                        cmrStatus = "racing";
                                        sendData("PRIVMSG", ex[2] + " " + BoldText(ColourChanger("The race will begin in 10 seconds!", "04")));
                                        bool five = false;
                                        bool four = false;
                                        bool three = false;
                                        bool two = false;
                                        bool one = false;
                                        bool go = false;
                                        countdown.Start();
                                        while (!go) {
                                            if (GetTime(countdown) == "00:00:05" && !five) {
                                                sendData("PRIVMSG", ex[2] + " " + BoldText(ColourChanger("5", "04")));
                                                five = true;
                                            }
                                            if (GetTime(countdown) == "00:00:06" && !four) {
                                                sendData("PRIVMSG", ex[2] + " " + BoldText(ColourChanger("4", "04")));
                                                four = true;
                                            }
                                            if (GetTime(countdown) == "00:00:07" && !three) {
                                                sendData("PRIVMSG", ex[2] + " " + BoldText(ColourChanger("3", "04")));
                                                three = true;
                                            }
                                            if (GetTime(countdown) == "00:00:08" && !two) {
                                                sendData("PRIVMSG", ex[2] + " " + BoldText(ColourChanger("2", "04")));
                                                two = true;
                                            }
                                            if (GetTime(countdown) == "00:00:09" && !one) {
                                                sendData("PRIVMSG", ex[2] + " " + BoldText(ColourChanger("1", "04")));
                                                one = true;
                                            }
                                            if (GetTime(countdown) == "00:00:10" && !go) {
                                                StartRace(racers, stahpwatch);
                                                sendData("PRIVMSG", ex[2] + " " + BoldText(ColourChanger("GO!", "04")));
                                                sendData("TOPIC", realRacingChan + " " + ":Status: IN PROGRESS | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                                countdown.Stop();
                                                go = true;
                                            }
                                        }
                                    } else {
                                        sendData("PRIVMSG", ex[2] + " " + "Not everyone is ready yet.");
                                    }
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.time": //Shows elapsed time in CMRs
                        #region
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            if (cmrStatus == "racing" || cmrStatus == "finished") //Command only works if CMR is open or finished
                                {
                                sendData("PRIVMSG", ex[2] + " " + GetTime(stahpwatch));
                            }
                        }
                        #endregion
                        break;

                    case ":.done": //When someone gets an SS on every CMR map
                        #region
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            if (cmrStatus == "racing") //Command only works if CMR is racing
                                {
                                if (GetStatus(racers, nickname) == 2) //Command only works if racer status is "racing"
                                    {
                                    //Set racer status to "done"
                                    SetTime(racers, nickname, stahpwatch);
                                    sendData("PRIVMSG", "TRAXBUSTER" + " " + ".proofcall " + getUserIgn(nickname));
                                    sendData("PRIVMSG", ex[2] + " " + nickname + " has finished in " + GetRanking(racers, nickname) + " place with a time of " + GetTime(stahpwatch) + ".");
                                    if (ComfirmTripleMassStatus(racers, 1, 4, 5)) //Stop the race if all racers are "done"/"quit"/"dq"
                                        {
                                        //Set race status to "finished"
                                        StopRace(stahpwatch);
                                        cmrStatus = "finished";
                                        sendData("TOPIC", realRacingChan + " " + ":Status: Complete | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                        sendData("PRIVMSG", mainchannel + " " + "Race Finished: Dustforce - Custom Map Race " + cmrId + " | " + GetNameRank(racers, 1) + " won with a time of " + GetTimeRank(racers, 1));
                                    }
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.undone": //Not quite done or continue racing after quitting
                        #region
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            if (cmrStatus == "racing") //Command only works if CMR is open
                                {
                                if (GetStatus(racers, nickname) == 1 || GetStatus(racers, nickname) == 4) //Command only works if racer status is "done" or "quit"
                                    {
                                    //Set racer status to "racing"
                                    SetStatus(racers, nickname, 2);
                                    sendData("PRIVMSG", ex[2] + " " + nickname + " isn't done yet.");
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.record": //Used to record a race, outputting the final results in .xlsx
                        #region
                        if (StringCompareNoCaps(nickname, "furkiepurkie")) {
                            if (cmrStatus == "finished") {
                                sendData("PRIVMSG", ex[2] + " Recording race...");
                                RecordResultsReddit(racers, cmrId);
                                RecordResultsJson(racers, UpdateJsonToDtMaps(cmrId), cmrId);
                                sendData("PRIVMSG", ex[2] + " Custom Map Race " + cmrId + " has been succesfully recorded!");
                            }
                        }
                        #endregion
                        break;

                    case ":.mappack":
                        #region
                        goto case ":.mappacks";
                    case ":.mappacks":
                        sendData("PRIVMSG", ex[2] + " Download map packs here: http://redd.it/279zmi");
                        #endregion
                        break;

                    case ":.faq":
                        //sendData("PRIVMSG", ex[2] + " Command: .faq <keyword>");
                        break;

                    //case ":.updatefaq":
                    //    #region
                    //    if (username.ToString() == "Furkiepurkie")
                    //    {
                    //        UpdateFaq(faq);
                    //        sendData("NOTICE", "Furkiepurkie" + " FAQ updated.");
                    //    }
                    //    #endregion
                    //    break;

                    //case ":.test":
                    //    UpdateJsonToDtMaps("35");
                    //    break;

                    case ":.unhype":
                        if (hype) {
                            sendData("PRIVMSG", ex[2] + " Aww :c");
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
                        sendData("PRIVMSG", ex[2] + " PONG");
                        break;
                    case ":.pong":
                        sendData("PRIVMSG", ex[2] + " PING");
                        break;
                    #endregion
                    case ":.updatebot": //doesn't actually update anything, just shuts down Furkiebot with a fancy update message, I always whisper this because it would look stupid to type a command like this in channel lol
                        if (StringCompareNoCaps(nickname, "furkiepurkie") || StringCompareNoCaps(nickname, "eklipz")) {
                            sendData("QUIT", " Updating FurkieBot (????)");
                        }
                        break;


                    //case ":.slap": //Im sorry
                    //    Slap(nickname, ex);
                    //    break;
                }
            }
            //Console.WriteLine("End no-params command switch " + parseTimer.Elapsed);






            if (ex.Length > 4) //Commands with parameters
                {
                string command = ex[3]; //grab the command sent

                switch (command) {
                    case ":.j61": // oin #channel
                        sendData("JOIN", ex[4]);
                        break;

                    case ":.p61": //Part #channel
                        sendData("PART", ex[4]);
                        break;

                    case ":.ign":
                        #region
                        string ign_ex4 = ex[4].TrimEnd(' ', '_');
                        if (StringCompareNoCaps(ign_ex4, getUserInfo(ign_ex4).ircname)) {
                            sendData("PRIVMSG", ex[2] + " " + "" + ColourChanger(ex[4].Trim(), "03") + " > " + ColourChanger(getUserInfo(ign_ex4).dustforcename, "03") + "");
                        } else {
                            sendData("PRIVMSG", ex[2] + " " + " No in-game name registered for " + ex[4].Trim() + "");
                        }
                        #endregion
                        break;

                    case ":.setign":
                        #region
                        string trimmedEx4 = ex[4].Trim();
                        string ircname = nickname;
                        char[] charsToRemove = { '_' };
                        ircname = ircname.TrimEnd(charsToRemove);

                        setUserIGN(ircname, trimmedEx4);
                        sendData("PRIVMSG", ex[2] + " New IGN registered: " + ColourChanger(ircname, "03") + " > " + ColourChanger(trimmedEx4, "03") + "");
                        #endregion
                        break;

                    case ":.comment": //Adds a comment after a racer is done
                        #region
                        if (GetStatus(racers, nickname) == 1 || GetStatus(racers, nickname) == 4) {
                            AddComment(racers, nickname, ex[4].ToString());
                            sendData("PRIVMSG", ex[2] + " Comment for " + nickname.Trim() + " added.");
                        }
                        #endregion
                        break;

                    case ":.dq": //DQ's a racer from race, should hardly be used, especially in combination with TRAXBUSTER, unless someone is clearly being a dick or something
                        #region
                        if (ex[2] == realRacingChan) //Command only works in racing channel
                            {
                            if (StringCompareNoCaps(nickname, "furkiepurkie")) {

                                DQEntrant(racers, ex[4], nickname);
                                sendData("PRIVMSG", ex[2] + " " + nickname + " disqualified PLACEHOLDER for reason: PLACEHOLDER");
                                if (ComfirmTripleMassStatus(racers, 1, 4, 5)) //Stop the race if all racers are "done"/"quit"/"dq"
                                    {
                                    //Set race status to "finished"
                                    StopRace(stahpwatch);
                                    cmrStatus = "finished";
                                    sendData("TOPIC", realRacingChan + " " + ":Status: Complete | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                    sendData("PRIVMSG", mainchannel + " " + "Race Finished: Dustforce - Custom Map Race " + cmrId + " | " + GetNameRank(racers, 1) + " won with a time of " + GetTimeRank(racers, 1));
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.setcmr": //Set CMR ID for whatever reason there might be
                        #region
                        sendData("PRIVMSG", ex[2] + " Custom Map Race has been set to " + ex[4]);
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"..\..\..\Data\CMR_ID.txt")) {
                            file.WriteLine(ex[4]);
                        }
                        #endregion
                        break;

                    case ":.quit61": //Quit 
                        #region
                        sendData("QUIT", ex[4]);
                        shouldRun = false; //turn shouldRun to false - the server will stop sending us data so trying to read it will not work and result in an error. This stops the loop from running and we will close off the connections properly
                        #endregion
                        break;

                    case ":.addmap": //Add map to CMR .cmrmaps command list
                        #region
                        if (true) {
                            int i = CountCertainCharacters(ex[4], ',');

                            if (i == 2 && nickname == "Furkiepurkie") //Gotta make sure the right parameters are used
                                {
                                string s = ",";
                                string chan = mainchannel;

                                /*
                                 * When a map is being approve, I just put 0 as mapid, which I later edit. 
                                 * [int mapid] should not be used once there's a better system for map submission.
                                */
                                int mapid = Convert.ToInt32(StringSplitter(ex[4], s)[0]);
                                string mapper = StringSplitter(ex[4], s)[1];
                                string mapname = StringSplitter(ex[4], s)[2];

                                AddCMRMap(cmrId, mapid, mapper, mapname);

                                sendData("PRIVMSG", chan + " New Maps added for CMR " + cmrId + ": \"" + mapname + "\" by " + mapper);
                                sendData("PRIVMSG", chan + " " + "Maps approved for CMR " + cmrId + " (" + UpdateJsonToDtMaps(cmrId).Rows.Count + "/6): " + GetCMRMaps(cmrId, UpdateJsonToDtMaps(cmrId)));
                            } else {
                                sendData("NOTICE", "Furkiepurkie" + " mapid,mapper,mapname");
                            }
                        }
                        #endregion
                        break;

                    case ":.delmap": //Not sure if this works, used to remove a map from the .cmrmaps command list
                        #region
                        if (StringCompareNoCaps(nickname, "furkiepurkie")) {
                            if (DeleteCmrMap(cmrId, ex[4])) {
                                sendData("PRIVMSG", ex[2] + " Map removed.");
                            } else {
                                sendData("PRIVMSG", ex[2] + " Map doesn't exist.");
                            }
                        }
                        #endregion
                        break;

                    case ":.editmapid": //
                        #region
                        if (true) {
                            int i = ex[4].Split(',').Length - 1; //Count amount of commas

                            if (i == 2 && StringCompareNoCaps(nickname, "furkiepurkie")) {
                                string s = ",";

                                /*
                                 * This is what I use to assign a mapid to an approved map. Since FurkieBot doesnt know how to get approved maps from Atlas, this is the way I do it. 
                                 * [int mapid] should not be used once there's a better system for map submission.
                                */
                                int mapid = Convert.ToInt32(StringSplitter(ex[4], s)[0]);
                                string mapper = StringSplitter(ex[4], s)[1];
                                string mapname = StringSplitter(ex[4], s)[2];

                                EditCMRMapId(cmrId, mapid, mapper, mapname);

                                sendData("NOTICE", nickname + @" http://" + "atlas.dustforce.com/" + mapid + " > \"" + mapname + "\" by " + mapper);
                            } else {
                                sendData("NOTICE", "Furkiepurkie" + " mapid,mapper,mapname");
                            }
                        }
                        #endregion
                        break;

                    case ":.forceunjoin": //You can force someone to .unjoin, please dont abuse your powers unless you are a troll
                        #region
                        if (StringCompareNoCaps(nickname, "furkiepurkie")) {
                            RemoveEntrant(racers, ex[4]);
                            string extraS = "";
                            if (CountEntrants(racers) != 1) {
                                extraS = "s";
                            }
                            sendData("PRIVMSG", ex[2] + " " + nickname + " removed " + ex[4] + " from the race. " + CountEntrants(racers) + " entrant" + extraS + ".");
                            sendData("MODE", realRacingChan + " -v " + ex[4]);
                        }
                        #endregion
                        break;

                    case ":.forcequit": //You can force someone to .quit, please dont abuse your powers unless you are a troll
                        #region
                        if (StringCompareNoCaps(nickname, "furkiepurkie")) {
                            SetStatus(racers, ex[4], 4);
                            sendData("PRIVMSG", ex[2] + " " + nickname + " forced " + ex[4] + " to forfeit from the race.");
                        }
                        #endregion
                        break;

                    case ":.forcedone": //You can force someone to .done, because sometimes, you just want to be able to guarentee that
                        #region
                        if (StringCompareNoCaps(nickname, "furkiepurkie") || StringCompareNoCaps(nickname, "eklipz")) {  // TODO REPLACE WITH ISADMIN FUNC
                            SetStatus(racers, ex[4], 1);
                            SetTime(racers, ex[4], stahpwatch);
                            sendData("PRIVMSG", ex[2] + " " + ex[4] + " has finished in " + GetRanking(racers, ex[4]) + " place with a time of " + GetTime(stahpwatch) + ".");
                            if (ComfirmTripleMassStatus(racers, 1, 4, 5)) //Stop the race if all racers are "done"/"quit"/"dq"
                                {
                                //Set race status to "finished"
                                StopRace(stahpwatch);
                                cmrStatus = "finished";
                                sendData("TOPIC", realRacingChan + " " + ":Status: Complete | Game: Dustforce | Goal: Custom Map Race " + cmrId + ". Download maps at http://atlas.dustforce.com/tag/custom-map-race-" + cmrId);
                                sendData("PRIVMSG", mainchannel + " " + "Race Finished: Dustforce - Custom Map Race " + cmrId + " | " + GetNameRank(racers, 1) + " won with a time of " + GetTimeRank(racers, 1));
                            }
                        }
                        #endregion
                        break;

                    case ":.forceundone": //You can force someone to .undone, get rekt thought you were done son?
                        #region
                        Console.WriteLine("Nickname: \t" + nickname);
                        if (StringCompareNoCaps(nickname, "furkiepurkie") || StringCompareNoCaps(nickname, "traxbuster")) {
                            Console.WriteLine("ex[2]: \t" + ex[2]);
                            if (ex[2] == realRacingChan || StringCompareNoCaps(ex[2], "furkiebot")) //Command only works in racing channel
                                {
                                Console.WriteLine("CMR status: \t" + cmrStatus);
                                if (cmrStatus == "racing") //Command only works if CMR is open
                                    {
                                    Console.WriteLine("Racer status: \t" + GetStatus(racers, ex[4].Trim()));
                                    if (GetStatus(racers, ex[4].Trim()) == 1 || GetStatus(racers, ex[4].Trim()) == 4) //Command only works if racer status is "done" or "quit"
                                        {
                                        //Set racer status to "racing"
                                        if (StringCompareNoCaps(nickname, "furkiepurkie")) {
                                            SetStatus(racers, nickname, 2);
                                            sendData("PRIVMSG", ex[2] + " " + ex[4].Trim() + " isn't done yet.");
                                        }
                                        if (StringCompareNoCaps(nickname, "traxbuster")) {
                                            string realnickname = getUserIrc(ex[4]);
                                            SetStatus(racers, realnickname, 2);
                                            sendData("PRIVMSG", realRacingChan + " " + "Nice try, " + getUserIgn(ex[4].Trim()) + "! Try to .done when you have an SS on " + BoldText("all") + " maps. You have been put back in racing status.");
                                            sendData("NOTICE", realnickname + " " + "If something went wrong and the proofcall is not justified, message Furkiepurkie about this issue.");
                                        }
                                    }
                                    Console.WriteLine("Racer status: \t" + GetStatus(racers, ex[4].Trim()));
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":sendmap": //FurkieBot loves private messages containing potential maps for a CMR!
                        goto case ":.sendmap";
                    case ":.sendmap":
                        #region
                        if (ex[1] == "PRIVMSG" && StringCompareNoCaps(ex[2], "furkiebot")) //Only private messages ofcourse
                            {
                            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"..\..\..\Data\Map_Request.txt", true)) {
                                file.WriteLine(nickname + " - " + ex[4]);
                            }
                            sendData("NOTICE", nickname + " You sent the following URL to me: " + ex[4]);
                            sendData("NOTICE", nickname + " Your map has been added to queue and will be reviewed as soon as possible (????)"); //Gotta thank your mappers :3
                            sendData("NOTICE", "Furkiepurkie " + nickname + " has added a map to queue: " + ex[4]);
                        }
                        #endregion
                        break;


                    case ":.maps":
                        goto case ":.cmrmaps";
                    case ":.cmrmaps":
                        #region
                        int value;
                        if (int.TryParse(ex[4], out value)) //I dont remember why I need to parse here
                            {
                            DataTable dt = UpdateJsonToDtMaps(ex[4]);
                            string maps = GetCMRMaps(ex[4], dt);
                            if (ex[4] != cmrId) {
                                if (Convert.ToInt32(dt.Rows[0]["mapid"]) != -1) {
                                    sendData("PRIVMSG", ex[2] + " " + "Maps used in CMR " + ex[4].Trim() + " (" + dt.Rows.Count + "): " + maps);
                                } else {
                                    sendData("PRIVMSG", ex[2] + " " + "No maps found.");
                                }
                            } else {
                                if (Convert.ToInt32(dt.Rows[0]["mapid"]) != -1) {
                                    sendData("PRIVMSG", ex[2] + " " + "Maps approved for CMR " + cmrId + " (" + dt.Rows.Count + "/6): " + maps);
                                } else {
                                    sendData("PRIVMSG", ex[2] + " " + "No maps submitted yet.");
                                }
                            }
                        }
                        #endregion
                        break;

                    case ":.saydf": //Can be used to broadcast a message to the mainchannel by whispering this command to FurkieBot
                        if (ex[1] == "PRIVMSG" && StringCompareNoCaps(ex[2], "furkiebot")) {
                            sendData("PRIVMSG", mainchannel + " " + ex[4]);
                        }
                        break;

                    case ":.sayracechan": //Can be used to broadcast a message to the racechannel by whispering this command to FurkieBot
                        if (ex[1] == "PRIVMSG" && StringCompareNoCaps(ex[2], "furkiebot")) {
                            sendData("PRIVMSG", realRacingChan + " " + ex[4]);
                        }
                        break;

                    case ":.kick": //Kick someone from a racingchannel
                        if (StringCompareNoCaps(nickname, "furkiepurkie")) {
                            sendData("KICK", ex[2] + " " + ex[4]);
                        }
                        break;

                    //case ":.test":
                    //    break;

                    #region .slap
                    case ":.slap": //A stupid command nobody asked for
                        Slap(nickname, ex);
                        break;
                    #endregion
                }
            }
            return shouldRun;
        }



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
                sendData("PRIVMSG", chan + " :" + " Upcoming race is Custom Map Race " + cmrId + ". There are only " + acceptedMaps.Count + " maps currently accepted, and we need at least " + MIN_MAPS + ".");
                sendData("PRIVMSG", chan + " :" + " It will happen on Saturday, " + saturday.Month + " " + saturday.Day.ToString() + @" at 6:30 pm GMT (conversion to your time here: http://www.timebie.com/std/gmt.php?q=18.5");
            } else {
                Console.WriteLine(DateTime.Now.TimeOfDay + "\t" + DateTime.Now.Date.ToString("dddd"));
                if (DateTime.Now.TimeOfDay < cmrtime && DateTime.Now.Date.ToString("dddd") == "Saturday") { //If it isnt CMR time yet
                    sendData("PRIVMSG", chan + " :" + "We have enough maps to start Custom Map Race " + cmrId + ", race can be initiated in "
                        + ColourChanger(nextCmrD + " days, "
                        + nextCmrH + " hours, "
                        + nextCmrM + " minutes and "
                        + nextCmrS + " seconds", "03") + ".");
                } else //If starting a race is possible
                                {
                    string extraS = "";
                    if (CountEntrants(racers) > 1) {
                        extraS = "s";
                    }
                    if (cmrStatus == "closed") //CMR race not opened yet
                                    {
                        sendData("PRIVMSG", chan + " :" + " Custom Map Race " + cmrId + " is available.");
                    }
                    if (cmrStatus == "open") //CMR race opened
                                    {
                        sendData("PRIVMSG", chan + " :Entry currently " + ColourChanger("OPEN", "03") + " for Custom Map Race " + cmrId + ". Join the CMR at " + ColourChanger(realRacingChan, "04") + ". " + CountEntrants(racers) + " entrants" + extraS);
                    }
                    if (cmrStatus == "racing") //CMR race ongoing
                                    {
                        sendData("NOTICE", nickname + " :Custom Map Race " + cmrId + " is currently " + ColourChanger("In Progress", "12") + " at " + ColourChanger(realRacingChan, "04") + ". " + CountEntrants(racers) + " entrant" + extraS);
                    }
                }
            }
        }



        /*
         * Slaps based on things.
         */
        private void Slap(string nickname, string[] ex) {
            Random r = new Random();
            int choice = r.Next(6);

            if (ex[4] == "me") {
                sendData("PRIVMSG", ex[3] + " :" + (char)1 + @"ACTION uses " + nickname + "'s own hands to slap himself. \"STOP HITTING YOURSELF, STOP HITTING YOURSELF!" + (char)1);
            } else if (IsAdmin(ex[4].ToLower())) {
                sendData("PRIVMSG", ex[3] + " :" + (char)1 + @"ACTION slaps " + nickname + ". Don't be like that!" + (char)1);
            } else {
                switch (choice) {
                    case 0:
                        sendData("PRIVMSG", ex[3] + " :" + (char)1 + @"ACTION slaps " + ex[4] + " with " + nickname + "'s favorite game console." + (char)1);
                        break;
                    case 1:
                        if (IsAdmin(nickname.ToLower())) {
                            goto case 4;
                        } else {
                            sendData("PRIVMSG", ex[3] + " :Only cool people are allowed to .slap people. Go slap yourself, " + nickname + ".");
                        }
                        break;
                    case 2:
                        sendData("PRIVMSG", ex[3] + " :" + (char)1 + @"ACTION slaps " + ex[4] + " around, just a little." + (char)1);
                        break;
                    case 3:
                        sendData("PRIVMSG", ex[3] + " :" + (char)1 + @"ACTION slaps " + ex[4] + " with vigor." + (char)1);
                        break;
                    case 4:
                        if (IsAdmin(nickname.ToLower())) {
                            sendData("PRIVMSG", ex[3] + " :" + (char)1 + @"ACTION slaps " + ex[4] + " with his cold, metal bot-hand" + (char)1);
                        } else {
                            sendData("PRIVMSG", ex[3] + " :Only cool people are allowed to .slap people. Go slap yourself, " + nickname + ".");
                        }
                        break;
                    case 5:
                        sendData("PRIVMSG", ex[3] + " :" + (char)1 + @"ACTION slaps " + nickname + ". BE NICE." + (char)1);
                        break;
                }
            }

        } /* Slap */



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





        PlayerInfo getUserInfo(string ircuser) {//[0] = ircname; [1] = dfname; [2] = rating
             //for (int i = 0; i < userlist.Rows.Count; i++) {
            //    string ircname = userlist.Rows[i]["ircname"].ToString();
            //    string dustforcename = userlist.Rows[i]["dustforcename"].ToString();
            //    string rating = userlist.Rows[i]["rating"].ToString();
            //    if (ircuser.ToLower().TrimEnd('_') == ircname) {
            //        res[0] = ircname;
            //        res[1] = dustforcename;
            //        res[2] = rating;
            //        userExist = true;
            //        i = userlist.Rows.Count;
            //    }
            //}
            PlayerInfo res = new PlayerInfo();
            if (userlist.ContainsKey(ircuser.ToLower())) {
                userlist.TryGetValue(ircuser.ToLower(), out res);
                return res;
            }

            return res;
        }






        string getUserIrc(string dustforceuser) {           // 
            PlayerInfo res = new PlayerInfo();
            if (dustforcelist.ContainsKey(dustforceuser.ToLower())) {
                dustforcelist.TryGetValue(dustforceuser.ToLower(), out res);
                return res.ircname;
            } else {
                return null;
            }
        }



        string getUserIgn(string ircuser) { return getUserInfo(ircuser.ToLower().TrimEnd('_')).ircname; }



        int getUserRating(string ircuser) { return getUserInfo(ircuser.ToLower()).rating; }


        /**
         * writes out the users file.
         */
        void WriteUsers() {
            //DataTable userlistCopy = userlist.Copy();
            //DataSet ds = new DataSet("ds");
            //ds.Namespace = "NetFrameWork";
            //ds.Tables.Add(userlistCopy);

            //ds.AcceptChanges();
            //string json = JsonConvert.SerializeObject(ds, Formatting.Indented);

            string json = JsonConvert.SerializeObject(userlist, Formatting.Indented);

            File.WriteAllText(@"..\..\..\Data\Userlist\userlistmap.json", json); // !! FILEPATH !!
        }




        void setUserIGN(string ircuser, string dustforceuser) {
            if (!dustforcelist.ContainsKey(dustforceuser.ToLower())) {
                PlayerInfo temp = new PlayerInfo();
                userlist.TryGetValue(ircuser.ToLower(), out temp);
                string oldname = temp.dustforcename;
                Console.WriteLine("name " + temp.ircname + " dustforcename " + temp.dustforcename + " tester " + temp.tester + " trusted " + temp.trusted + " admin " + temp.admin);

                //delete old dustforceuser entry
                if (dustforcelist.ContainsKey(oldname.ToLower())) {
                    dustforcelist.Remove(oldname.ToLower());
                }
                if (userlist.ContainsKey(ircuser.ToLower())) {
                    userlist.Remove(ircuser.ToLower());
                }
                temp.dustforcename = dustforceuser.ToLower();
                userlist.Add(ircuser.ToLower(), temp);
                dustforcelist.Add(dustforceuser.ToLower(), temp);


                WriteUsers();
            } else {
                sendData("NOTICE", ircuser + " :Someone already has that IGN (" + dustforceuser + ") registered. You may have registered it to another irc name?");
            }
        }



        //void setUserInfo(string ircuser, string dustforceuser, int rating) {
        //    bool userExist = false;

        //    for (int i = 0; i < userlist.Rows.Count; i++) {
        //        if (ircuser.ToLower().TrimEnd('_') == userlist.Rows[i]["ircname"].ToString()) {
        //            userlist.Rows[i]["dustforcename"] = dustforceuser;
        //            userExist = true;
        //            i = userlist.Rows.Count;
        //        }
        //    }
        //    if (!userExist) {
        //        userlist.Rows.Add(ircuser.ToLower().TrimEnd('_'), dustforceuser, rating);
        //    }

        //    DataTable dtCopy = userlist.Copy();
        //    DataSet ds = new DataSet("ds");
        //    ds.Namespace = "NetFrameWork";
        //    ds.Tables.Add(dtCopy);

        //    ds.AcceptChanges();

        //    string json = JsonConvert.SerializeObject(ds, Formatting.Indented);

        //    File.WriteAllText(@"..\..\..\Data\Userlist\userlist.json", json); // !! FILEPATH !!
        //}




        bool AddEntrant(string racer) //Used to add a racer to entrants
        {
            racers.Rows.Add(racer, 6, 0, 0, 0, 0, "", getUserRating(racer)); //name, status, hour, min, sec, 10th sec, comment, rating
            return true;
        } /* AddEntrant */




















        static string GetCurrentCMRID() {//Used to fetch the current CMR number
            string[] id = System.IO.File.ReadAllLines(@"..\..\..\Data\CMR_ID.txt"); // !! FILEPATH !!
            return id[0];
        } /* GetCurrentCMRID() */



        static string GetCurrentCMRStatus() {//Used to fetch current CMR status
            string[] id = System.IO.File.ReadAllLines(@"..\..\..\Data\CMR_STATUS.txt"); // !! FILEPATH !!
            return id[0];
        } /* GetCurrentCMRStatus */



        static void SetCurrentCMRStatus(string s) {//Used to either open or close a CMR
            string text = s;
            System.IO.File.WriteAllText(@"..\..\..\Data\CMR_STATUS.txt", s); // !! FILEPATH !!
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



        static long CmrMapCount(string cmrid) //Used to count the amount of maps on the current CMR
        {
            long count = UpdateJsonToDtMaps(cmrid).Rows.Count;
            return count;
        } /* CountMapsInCMR() */



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



        static void StartRace(DataTable racers, Stopwatch s) //Starts running timer
        {
            if (CountEntrants(racers) > 0) {
                s.Start();
                foreach (DataRow dr in racers.Rows) {
                    dr["Status"] = 2;
                }
            }
        } /* StartTime()*/



        static void StopRace(Stopwatch s) //Starts running timer
        {
            s.Stop();
        } /* StopTime() */



        static string GetNameRank(DataTable racers, int rank) {
            string res = "";
            if (CountEntrants(racers) > 0) {
                DataView dv = racers.DefaultView;
                dv.Sort = "Status, Hour, Min, Sec, TSec";
                racers = dv.ToTable();
                res = racers.Rows[rank - 1]["Name"].ToString();
            }
            return res;
        }



        static string GetTimeRank(DataTable racers, int rank) {
            string res = "";
            if (CountEntrants(racers) > 0) {
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



        static int CountEntrants(DataTable racers) //Used to count the amount entrants in the current CMR
        {
            return racers.Rows.Count;
        } /* CountEntrants() */



        static string GetEntrants(DataTable racers, Stopwatch timer) //Used to get one single string of entrants
        {
            string result = "";
            if (CountEntrants(racers) > 0) {
                DataView dv = racers.DefaultView;
                dv.Sort = "Status, Hour, Min, Sec, TSec, Name";
                racers = dv.ToTable();
                for (int i = 0; i < CountEntrants(racers); i++) {
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
                    if ((i + 1) != CountEntrants(racers)) //If currentracer isnt the last one on the list add a "|" and start over
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



        static bool CheckEntrant(DataTable racers, string racer) //Checks if a certain user has entered the race
        {
            var foundRows = racers.Select("Name = '" + racer + "'");
            if (foundRows.Length != 0) {
                return true; //user found
            } else {
                return false; //user not found
            }
        } /* SearchEntrant */



        static void DQEntrant(DataTable racers, string racerreason, string mod) {
            string txt = racerreason;

            string re1 = "((?:[a-z][a-z0-9_]*))";	// Variable Name 1

            Regex r = new Regex(re1, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match m = r.Match(txt);
            string racer = m.ToString();
            string reason = mod + ": " + racerreason.Remove(0, racer.Length + 1);

            SetStatus(racers, racer, 5);
            AddComment(racers, racer, reason);
        }



        static int GetStatus(DataTable racers, string racer) //
        {
            int status = 0;
            if (CheckEntrant(racers, racer)) { //If racer exists in race
                for (int i = 0; i < CountEntrants(racers); i++) {
                    string name = racer;
                    if (racers.Rows[i]["Name"].ToString() == racer) {
                        status = Convert.ToInt32(racers.Rows[i]["Status"]);
                    }
                }
            }
            return status;
        }



        static bool ComfirmMassStatus(DataTable racers, int status) { //Checks if the whole list of racers share the same status
            bool get = true;
            if (CountEntrants(racers) > 0) {
                for (int i = 0; i < CountEntrants(racers); i++) {
                    int s = Convert.ToInt32(racers.Rows[i]["Status"]);
                    if (s != status) {
                        get = false;
                        break;
                    }
                }
            }
            return get;
        }



        static bool ComfirmDoubleMassStatus(DataTable racers, int s1, int s2) //Checks if the whole list of racers share the same status
        {
            bool get = true;
            if (CountEntrants(racers) > 0) {
                for (int i = 0; i < CountEntrants(racers); i++) {
                    int s = Convert.ToInt32(racers.Rows[i]["Status"]);
                    if (s != s1 && s != s2) {
                        get = false;
                        break;
                    }
                }
            }
            return get;
        }



        static bool ComfirmTripleMassStatus(DataTable racers, int s1, int s2, int s3) { //Checks if the whole list of racers share the same status. 
            bool get = true;
            if (CountEntrants(racers) > 0) {
                for (int i = 0; i < CountEntrants(racers); i++) {
                    int s = Convert.ToInt32(racers.Rows[i]["Status"]);
                    if (s != s1 && s != s2 && s != s3) {
                        get = false;
                        break;
                    }
                }
            }
            return get;
        }



        static bool SetStatus(DataTable racers, string racer, int newStatus) //Sets status of a racer
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



        static int CountStatus(DataTable racers, int status) {
            int count = 0;
            foreach (DataRow dr in racers.Rows) {
                if (Convert.ToInt32(dr["Status"]) == status) {
                    count++;
                }
            }
            return count;
        }



        static bool SetTime(DataTable racers, string racer, Stopwatch timer) //Sets time on a racer that .done
        {
            if (CheckEntrant(racers, racer)) //If user exists in race
            {
                foreach (DataRow dr in racers.Rows) {
                    if (dr["Name"].ToString() == racer && Convert.ToInt32(dr["Status"]) == 2) {
                        dr["TSec"] = GetTimeTSec(timer);
                        dr["Sec"] = GetTimeSec(timer);
                        dr["Min"] = GetTimeMin(timer);
                        dr["Hour"] = GetTimeHour(timer);
                        SetStatus(racers, racer, 1);
                    }
                }
                return true;
            } else {
                return false;
            }
        } /* SetTime */



        static void RemoveEntrant(DataTable racers, string racer) //Get that fool outta there
        {
            for (int i = 0; i < CountEntrants(racers); i++) {
                string name = racers.Rows[i]["Name"].ToString();
                if (name == racer) {
                    racers.Rows[i].Delete();
                }
            }
            racers.AcceptChanges();
        } /* RemoveEntrant */



        static string GetRanking(DataTable racers, string racer) //Used to get proper ranks like 1st, 2nd, 3rd etc.
        {
            int r = 0;
            DataView dv = racers.DefaultView;
            dv.Sort = "Status, Hour, Min, Sec, TSec";
            racers = dv.ToTable();
            for (int i = 0; i < CountEntrants(racers); i++) {
                string name = racers.Rows[i]["Name"].ToString();
                if (name == racer) {
                    r = i + 1;
                }
            }
            int rest = 0;
            while (r > 10) {
                r = r - 10;
                rest += 10;
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



        static void AddComment(DataTable racers, string racer, string comment) {
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



        /* // YAY WE NOW NO LONGER RELY ON READING FROM JSON ALL THE TIME
         * static DataTable UpdateJsonUserlist() {
            string filepath = @"..\..\..\Data\Userlist\userlist.json"; // !! FILEPATH !!
            string[] jsonarray = File.ReadAllLines(filepath);
            string json = string.Join("", jsonarray);

            DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);
            DataTable dt = ds.Tables["userlist"];

            return dt;
        }*/




        static DataTable UpdateFaqList() {
            string filepath = @"..\..\..\Data\FAQ\faq.json"; // !! FILEPATH !!
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



        static DataTable UpdateJsonToDtMaps(string cmrid) {
            string filepath = @"..\..\..\Data\CMR Data\Maps\CMR" + cmrid + "Maps.json"; // !! FILEPATH !!
            Console.WriteLine(filepath);

            if (File.Exists(filepath)) {
                string[] jsonarray = File.ReadAllLines(filepath);
                string json = string.Join("", jsonarray);

                DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);

                DataTable dt = ds.Tables["maps"];

                foreach (DataRow dr in dt.Rows) {
                    Console.WriteLine(dr["mapid"] + " - " + dr["mapper"] + " - " + dr["mapname"]);
                }

                return dt;
            } else {
                DataTable dt = new DataTable();
                dt.Columns.Add("mapid", typeof(int));
                dt.Columns.Add("mapper", typeof(string));
                dt.Columns.Add("mapname", typeof(string));
                dt.Rows.Add(-1, "-1", "-1");
                return dt;
            }
        }



        static bool DeleteCmrMap(string cmrid, string mapname) {
            bool res = false;

            string filepath = @"..\..\..\Data\CMR Data\Maps\CMR" + cmrid + "Maps.json"; // !! FILEPATH !!

            if (File.Exists(filepath)) {
                string[] jsonarray = File.ReadAllLines(filepath);
                string json = string.Join("", jsonarray);

                DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);

                DataTable dt = ds.Tables["maps"];

                for (int i = 0; i < dt.Rows.Count; i++) {
                    string name = dt.Rows[i]["mapname"].ToString();
                    if (name == mapname) {
                        dt.Rows[i].Delete();
                        res = true;
                    }
                }
                dt.AcceptChanges();

                string json2 = JsonConvert.SerializeObject(ds, Formatting.Indented);
                System.IO.File.WriteAllText(@"..\..\..\Data\CMR Data\Maps\CMR" + cmrid + "Maps.json", json); // !! FILEPATH !!
            }

            return res;
        }



        static void AddCMRMap(string cmrid, int mapid, string mapper, string mapname) {
            DataTable dtCopy = UpdateJsonToDtMaps(cmrid).Copy();
            DataSet ds = new DataSet("ds");
            ds.Namespace = "NetFrameWork";
            ds.Tables.Add(dtCopy);
            dtCopy.Rows.Add(mapid, mapper, mapname);

            ds.AcceptChanges();

            string json = JsonConvert.SerializeObject(ds, Formatting.Indented);

            System.IO.File.WriteAllText(@"..\..\..\Data\CMR Data\Maps\CMR" + cmrid + "Maps.json", json); // !! FILEPATH !!
        }



        static void EditCMRMapId(string cmrid, int mapid, string mapper, string mapname) {
            DataTable dtCopy = UpdateJsonToDtMaps(cmrid).Copy();
            DataSet ds = new DataSet("ds");
            ds.Namespace = "NetFrameWork";
            ds.Tables.Add(dtCopy);

            foreach (DataRow dr in dtCopy.Rows) {
                if (dr["mapname"].ToString() == mapname) {
                    dr["mapid"] = mapid;
                }
            }

            ds.AcceptChanges();

            string json = JsonConvert.SerializeObject(ds, Formatting.Indented);

            System.IO.File.WriteAllText(@"..\..\..\Data\CMR Data\Maps\CMR" + cmrid + "Maps.json", json); // !! FILEPATH !!
        }



        static string JsonToDatatableMaps2(DataTable dt, string cmrid, string irccommand, string ircchannel) {
            string res = "";

            if (File.Exists(@"..\..\..\Data\CMR Results\CMR" + cmrid + "Results.json")) //Check if CMR number exists // !! FILEPATH !!
            {
                string[] path = System.IO.File.ReadAllLines(@"..\..\..\Data\CMR Results\CMR" + cmrid + "Results.json"); // !! FILEPATH !!
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



        static void RecordResultsJson(DataTable racers, DataTable maps, string cmrid) {
            DataView dv = racers.DefaultView;
            dv.Sort = "Status, Hour, Min, Sec, TSec, Name";
            racers = dv.ToTable();

            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw)) {
                writer.Formatting = Formatting.Indented;

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

            System.IO.File.WriteAllText(@"..\..\..\Data\CMR Results\CMR" + cmrid + @"Results.json", sb.ToString()); // !! FILEPATH !!
        }



        static void RecordResultsReddit(DataTable racers, string cmrid) {
            DataView dv = racers.DefaultView;
            dv.Sort = "Status, Hour, Min, Sec, TSec, Name";
            racers = dv.ToTable();

            string[] lines = new string[racers.Rows.Count + 2];

            lines[0] = @"|Rank|Name|Time|Comment|Rating";
            lines[1] = @"|:-|:-|:-|:-|:-|";

            for (int i = 0; i < racers.Rows.Count; i++) {
                string name = racers.Rows[i]["Name"].ToString();
                int status = Convert.ToInt32(racers.Rows[i]["Status"]);
                string rank = GetRanking(racers, name);
                int hour = Convert.ToInt32(racers.Rows[i]["Hour"]);
                int min = Convert.ToInt32(racers.Rows[i]["Min"]);
                int sec = Convert.ToInt32(racers.Rows[i]["Sec"]);
                TimeSpan time = new TimeSpan(hour, min, sec);
                string comment = racers.Rows[i]["Comment"].ToString();
                string rating = racers.Rows[i]["Rating"].ToString();

                lines[i + 2] = "|" + rank + "|" + name + "|" + time.ToString(@"%h\:mm\:ss") + "|" + comment + "|" + rating + "|";
            }

            System.IO.File.WriteAllLines(@"..\..\..\Data\CMR Results\Reddit" + cmrid + @".txt", lines); // !! FILEPATH !!
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



        static bool CheckSS(string racer, string cmrid) {
            DataTable maps;
            maps = UpdateJsonToDtMaps(cmrid).Copy();

            bool res = false;

            int mapsCount = maps.Rows.Count;

            string[] score = new string[mapsCount - 1];

            for (int i = 0; i < mapsCount; i++) {
                DataTable scores;
                scores = ReadApiLeaderboardToDt(ReadApiLeaderboardToJson(maps.Rows[i]["mapname"].ToString(), Convert.ToInt32(maps.Rows[i]["mapid"]), 0)).Copy();
            }

            return res;
        }



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
            dt.Rows.Add("Marksel", 0, 45, 30, 0, 0);

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

    } /* IRCBot */



    internal class Program {
        private static void Main(string[] args) {
            IRCConfig conf = new IRCConfig();
            conf.name = "FurkieBot";
            conf.nick = "FurkieBot_";
            conf.altNick = "FurkieBot_";
            conf.port = 6667;
            conf.server = "irc2.speedrunslive.com";
            conf.pass = "ilovecalistuslol";
            using (var bot = new FurkieBot(conf)) {
                bot.Connect();
                bot.IRCWork();
            }
            Console.WriteLine("Furkiebot quit/crashed");
            Console.ReadLine();
        } /* Main() */
    } /* Program */
}