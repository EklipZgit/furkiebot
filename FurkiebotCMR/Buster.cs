using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Data;
//using System.Data.OleDb;
//using DocumentFormat.OpenXml;
//using System.IO.Packaging; //WindowsBase.dll
using System.Net;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using FurkiebotCMR;

namespace TraxBusterCMR {

    public class TraxBuster : IDisposable {

        /**<summary>
         * Holds the currently running instance of FurkieBot that called this instance of TraxBuster.
         * </summary>
         */
        private FurkieBot furkiebot;
        private bool exit = false;

        private TcpClient IRCConnection = null;
        private IRCConfig config;
        private NetworkStream ns = null;
        private StreamReader sr = null;
        private StreamWriter sw = null;


        public TraxBuster(IRCConfig config, FurkieBot furkiebot) {
            this.config = config;
            this.furkiebot = furkiebot;
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
                sr = new StreamReader(ns);
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
                sw.WriteLine(cmd + " " + param);
                sw.Flush();
                Console.WriteLine(cmd + " " + param);
            }
        }  /* sendData() */


        public void NotifyExit() {
            this.exit = true;
        }







        public void IRCWork() {
            string sep = ColourChanger(" | ", "07");

            int cmrid = 35;
            DataTable maps = UpdateJsonToDtMaps(cmrid).Copy();

            string[] ex;
            string data;

            while (!exit) {
                data = sr.ReadLine();
                if (data != "PING :irc2.speedrunslive.com" || data != "PONG :irc2.speedrunslive.com")
                    Console.WriteLine(data);
                char[] charSeparator = new char[] { ' ' };
                ex = data.Split(charSeparator, 5);

                string inputt = ex[0];
                string re22 = "((?:[a-z][a-z0-9_]*))";
                Regex rr = new Regex(re22, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                Match usernamee = rr.Match(inputt);
                string nickname = usernamee.ToString();


                string op = ex[1];
                string chan = "";
                if (ex.Length > 2) {
                    chan = ex[2];
                }





                if (ex[0] == "PING") //Pinging server in order to stay connected
                    {
                    sendData("PONG", ex[1]);
                }




                //Events
                switch (op) {
                    case "001": //Autojoin channel when first response from server
                        sendData("JOIN", furkiebot.realRacingChan);
                        sendData("PRIVMSG", "NickServ" + " ghost " + config.nick + " " + config.pass);
                        sendData("JOIN", furkiebot.cmrchannel);
                        break;
                }


                if (ex.Length == 4) //Commands without parameters
                {
                    string command = ex[3]; //grab the command sent

                    switch (command) {
                        case ":.mapcount":

                            break;


                        case ":.exit":
                            if (furkiebot.IsAdmin(nickname.ToLower(), nickname)) {
                                this.exit = true;
                            }
                            break;
                    }

                }

                if (ex.Length > 4) //Commands with parameters
                {
                    string command = ex[3]; //grab the command sent

                    switch (command) {
                        case ":.join001":
                            if (StringCompareNoCaps(nickname, FurkieBot.BOT_NAME) || furkiebot.IsAdmin(nickname.ToLower(), nickname)) {
                                sendData("JOIN", ex[4]);
                            }
                            break;

                        case ":.proofcall":
                            if (StringCompareNoCaps(nickname, FurkieBot.BOT_NAME) || furkiebot.IsAdmin(nickname, nickname)) {
                                Console.WriteLine("Proofcall START for " + ex[4]);
                                string[] proofcallData = new string[maps.Rows.Count];
                                string[] ex2;
                                string list = "";
                                bool undone = false;

                                char[] seperator = new char[] { ',' };

                                int i = 0;
                                foreach (DataRow dr in maps.Rows) {
                                    proofcallData[i] = CheckSSTest(ex[4], dr["mapname"].ToString(), Convert.ToInt32(dr["mapid"]));
                                    ex2 = proofcallData[i].Split(seperator, 3);
                                    if (proofcallData[i] != "Level not found.") {
                                        Console.WriteLine(ex2[0] + " = " + ex2[1]);
                                        if (ex2[1] != "SS") {
                                            list += ex2[0] + ", ";
                                            if (!undone) {
                                                sendData("PRIVMSG", "FurkieBot " + ".forceundone " + ex[4].Trim());
                                                undone = true;
                                            }
                                        }
                                    } else {
                                        Console.WriteLine("Invalid map data!");
                                    }
                                    i++;
                                }
                                if (list != "") {
                                    sendData("PRIVMSG", "FurkieBot " + ".sayracechan " + ex[4] + " doesn't have an SS on the following maps: " + list.TrimEnd(',', ' '));
                                } else {
                                    //Do Nothing
                                }
                                Console.WriteLine("Proofcall END");
                            }
                            break;
                    }
                }
            }
        } /* IRCWork() */

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

        static int GetCmrId() {
            int res;
            string filepath = @"C:\CMR\Data\CMR_ID.txt";
            res = Convert.ToInt32(File.ReadAllText(filepath));
            return res;
        }

        static string[] StringSplitter(string s, string v) //Seperate a string in an array of strings
        {
            string[] separators = { @v };
            string value = @s;
            string[] words = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return words;
        } /* StringSplitter() */

        static string BoldText(string s) {
            string text = s;
            text = (char)2 + s + (char)2;
            return text;
        }

        static string ColourChanger(string s, string colour) //Used to colourcode text in irc
        {
            //0 white
            //1 black
            //2 blue (navy)
            //3 green
            //4 red
            //5 brown (maroon)
            //6 purple
            //7 orange (olive)
            //8 yellow
            //9 light green (lime)
            //10 teal (a green/blue cyan)
            //11 light cyan (cyan) (aqua)
            //12 light blue (royal)
            //13 pink (light purple) (fuchsia)
            //14 grey
            //15 light grey (silver)

            string text = s;
            text = (char)3 + colour + s + (char)3;
            return text;
        } /* ColourChanger */

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

        static DataTable UpdateJsonToDtMaps(int cmrid) {
            string filepath = @"C:\CMR\Maps\CMR" + cmrid + "Maps.json";

            if (File.Exists(filepath)) {
                string[] jsonarray = File.ReadAllLines(filepath);
                string json = string.Join("", jsonarray);

                DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);

                DataTable dt = ds.Tables["maps"];

                //foreach (DataRow dr in dt.Rows)
                //{
                //    Console.WriteLine(dr["mapid"] + " - " + dr["mapper"] + " - " + dr["mapname"]);
                //}

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

        static string ReadApiLeaderboardToJson(string mapname, int mapid, int page) {
            using (var w = new WebClient()) {
                var json_data = string.Empty;

                try {
                    string realname = mapname.Replace(" ", "-");
                    int realpage = page * 10;
                    json_data = w.DownloadString(@"http://" + @"df.hitboxteam.com/backend6/scores.php?level=" + realname + @"-" + mapid + @"&offset=" + realpage + @"&max=10");
                } catch (Exception) { }

                return json_data;
            }
        }

        static DataTable ReadApiLeaderboardToDt(string json) {
            DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);

            DataTable dt = ds.Tables["scorelist"];

            return dt;
        }

        static string CheckSSTest(string user, string mapname, int mapid) {
            string res = "";
            int page = 0;

            while (res == "") {
                string json = ReadApiLeaderboardToJson(mapname, mapid, page);

                JObject rss = JObject.Parse(json);

                if (json == "{\"error\":\"Level not found.\"}") {
                    res = "Level not found.";
                } else {
                    if (rss["best_scores"].ToString() != "[]") {
                        var query =
                            from p in rss["best_scores"]
                            where (string)p["name"] == user
                            select new {
                                finesse = (string)p["score_finesse"],
                                thoroughness = (string)p["score_thoroughness"],
                                timestamp = UnixTimeStampToDateTime((double)p["timestamp"])
                            };

                        foreach (var item in query) {
                            res = mapname + "," + item.finesse + item.thoroughness + "," + item.timestamp;
                        }

                        if (res == "") {
                            page++;
                        }
                    } else {
                        res = mapname + @",No Score,No Time";
                    }
                }
            }
            return res;
        }

        static bool CheckSS(string racer, int cmrid) {
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

        static DateTime UnixTimeStampToDateTime(double unixTimeStamp) {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        static DataTable UpdateJsonUserlist() {
            string filepath = @"C:\Users\Furkan Pham\Documents\FurkieBot\Data\Userlist\userlist.json";
            string[] jsonarray = File.ReadAllLines(filepath);
            string json = string.Join("", jsonarray);

            DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);

            DataTable dt = ds.Tables["userlist"];

            return dt;
        }

        static string[] GetUserInfo(string ircuser) //[0] = irc; [1] = df; [2] = rating
        {
            string[] res = { "", "", "" };
            DataTable dt = UpdateJsonUserlist();

            for (int i = 0; i < dt.Rows.Count; i++) {
                string ircname = dt.Rows[i]["ircname"].ToString();
                string dustforcename = dt.Rows[i]["dustforcename"].ToString();
                string rating = dt.Rows[i]["rating"].ToString();
                if (ircuser.ToLower() == ircname) {
                    res[0] = ircname;
                    res[1] = dustforcename;
                    res[2] = rating;
                    i = dt.Rows.Count;
                }
            }
            return res;
        }
        static string GetUserIrc(string dustforceuser) {
            string res = "";
            DataTable dt = UpdateJsonUserlist();

            for (int i = 0; i < dt.Rows.Count; i++) {
                string ircname = dt.Rows[i]["ircname"].ToString();
                string dustforcename = dt.Rows[i]["dustforcename"].ToString();
                if (dustforceuser == dustforcename) {
                    res = dustforcename;
                    i = dt.Rows.Count;
                }
            }
            return res;
        }
        static string GetUserIgn(string ircuser) { return GetUserInfo(ircuser)[1]; }
        static int GetUserRating(string ircuser) { return Convert.ToInt32(GetUserInfo(ircuser)[2]); }

    } /* IRCBot */
}