/**
 * Buster.cs
 * Class for the secondary IRC bot, TRAXBUSTER. Used in races to disqualify
 * participants who did not actually complete all the maps. May be replaced
 * in the future by a simple FurkieBot thread.
 * @author Furkan Pham (Furkiepurkie)
 */

using FurkiebotCMR;
using MapCMR;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using UserCMR;

namespace TraxBusterCMR
{

    public class TraxBuster : IDisposable
    {

        ///<summary>
        ///  Holds the currently running instance of FurkieBot that called this instance of TraxBuster.
        ///</summary>
        private FurkieBot furkiebot;
        private bool exit = false;

        private TcpClient IRCConnection = null;
        private IRCConfig config;
        private NetworkStream ns = null;
        private StreamReader sr = null;
        private StreamWriter sw = null;
        private UserManager UserMan = UserManager.Instance;
        private MapManager MapMan = MapManager.Instance;



        public TraxBuster(IRCConfig config, FurkieBot furkiebot)
        {
            this.config = config;
            this.furkiebot = furkiebot;
        } /* IRCBot */

        public void Connect()
        {
            try
            {
                IRCConnection = new TcpClient(config.server, config.port);
            }
            catch
            {
                Console.WriteLine("Connection Error");
                throw;
            }

            try
            {
                ns = IRCConnection.GetStream();
                sr = new StreamReader(ns);
                sw = new StreamWriter(ns);
                sendData("USER", config.nick + " 0 * :" + config.name);
                sendData("NICK", config.nick);
                sendData("PASS", config.pass);
            }
            catch
            {
                Console.WriteLine("Communication error");
                throw;
            }
        }  /* Connect() */

        public void sendData(string cmd, string param)
        {
            if (param == null)
            {
                sw.WriteLine(cmd);
                sw.Flush();
                Console.WriteLine(cmd);
            }
            else
            {
                sw.WriteLine(cmd + " " + param);
                sw.Flush();
                Console.WriteLine(cmd + " " + param);
            }
        }  /* sendData() */


        public void NotifyExit()
        {
            this.exit = true;
        }







        public void IRCWork()
        {
            string sep = FurkieBot.ColourChanger(" | ", "07");

            string[] ex;
            string data;

            while (!exit)
            {
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
                if (ex.Length > 2)
                {
                    chan = ex[2];
                }





                if (ex[0] == "PING") //Pinging server in order to stay connected
                {
                    sendData("PONG", ex[1]);
                }




                //Events
                switch (op)
                {
                    case "001": //Autojoin channel when first response from server
                        sendData("JOIN", furkiebot.realRacingChan);
                        sendData("PRIVMSG", "NickServ" + " ghost " + config.nick + " " + config.pass);
                        sendData("JOIN", furkiebot.cmrchannel);
                        break;
                }


                if (ex.Length == 4) //Commands without parameters
                {
                    string command = ex[3]; //grab the command sent

                    switch (command)
                    {
                        case ":.mapcount":

                            break;


                        case ":.exit":
                            if (UserMan.IsAdmin(nickname.ToLower()))
                            {
                                this.exit = true;
                            }
                            break;
                    }

                }

                if (ex.Length > 4) //Commands with parameters
                {
                    string command = ex[3]; //grab the command sent
                    string botName = ConfigurationManager.AppSettings["BotName"];
                    switch (command)
                    {
                        case ":.join":
                            if (StringCompareNoCaps(nickname, botName) || UserMan.IsAdmin(nickname, nickname))
                            {
                                sendData("JOIN", ex[4]);
                            }
                            break;

                        case ":.proofcall":
                            string racer = ex[4].Trim();
                            if (StringCompareNoCaps(nickname, botName) || UserMan.IsAdmin(nickname, nickname))
                            {
                                Console.WriteLine("Proofcall START for " + ex[4]);
                                //TODO now that Leaderboard grabbing stuff is done....

                            }


                            // **OLD**  **OLD**  **OLD**  **OLD**  **OLD**  **OLD**  **OLD**  **OLD**  **OLD**  

                            //if (StringCompareNoCaps(nickname, FurkieBot.BOT_NAME) || UserMan.IsAdmin(nickname, nickname)) {
                            //	Console.WriteLine("Proofcall START for " + ex[4]);
                            //	string[] proofcallData = new string[maps.Rows.Count];
                            //	string[] ex2;
                            //	string list = "";
                            //	bool undone = false;

                            //	char[] seperator = new char[] { ',' };

                            //	int i = 0;
                            //	foreach (DataRow dr in maps.Rows) {
                            //		proofcallData[i] = CheckSS(ex[4], dr["mapname"].ToString(), Convert.ToInt32(dr["mapid"]));
                            //		ex2 = proofcallData[i].Split(seperator, 3);
                            //		if (proofcallData[i] != "Level not found.") {
                            //			Console.WriteLine(ex2[0] + " = " + ex2[1]);
                            //			if (ex2[1] != "SS") {
                            //				list += ex2[0] + ", ";
                            //				if (!undone) {
                            //					sendData("PRIVMSG", "FurkieBot " + ".forceundone " + ex[4].Trim());
                            //					undone = true;
                            //				}
                            //			}
                            //		} else {
                            //			Console.WriteLine("TRAXBUSTER: Invalid map data!");
                            //		}
                            //		i++;
                            //	}
                            //	if (list != "") {
                            //		furkiebot.Msg(FurkieBot.BOT_NAME, ".sayracechan " + ex[4] + " doesn't have an SS on the following maps: " + list.TrimEnd(',', ' '));
                            //	} else {
                            //		//Do Nothing
                            //	}
                            //	Console.WriteLine("Proofcall END");
                            //}
                            break;
                    }
                }
            }
        } /* IRCWork() */



        public void Dispose()
        {
            if (sr != null)
                sr.Close();
            if (sw != null)
                sw.Close();
            if (ns != null)
                ns.Close();
            if (IRCConnection != null)
                IRCConnection.Close();
        } /* Dispose() */



        static int GetCmrId()
        {
            int res;
            string filepath = @"C:\CMR\Data\CMR_ID.txt";
            res = Convert.ToInt32(File.ReadAllText(filepath));
            return res;
        }


        //Seperate a string in an array of strings
        static string[] StringSplitter(string s, string v)
        {
            string[] separators = { @v };
            string value = @s;
            string[] words = value.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            return words;
        } /* StringSplitter() */


        //type 1 = chars and digits, type 2 = digits, type 3 = dice
        static string RandomCharGenerator(int length, int type)
        {
            string valid = "";
            if (type == 1)
            {
                valid = "abcdefghijklmnopqrstuvwxyz1234567890";
            }
            if (type == 2)
            {
                valid = "1234567890";
            }
            if (type == 3)
            {
                valid = "123456";
            }
            string res = "";
            Random rnd = new Random();
            while (0 < length--)
                res += valid[rnd.Next(valid.Length)];
            return res;
        } /* RandomChannelGenerator */

        static bool StringCompareNoCaps(string s1, string s2)
        {
            return string.Equals(s1, s2, StringComparison.CurrentCultureIgnoreCase);
        }

        static DataTable UpdateJsonToDtMaps(int cmrid)
        {
            string filepath = @"C:\CMR\Maps\CMR" + cmrid + "Maps.json";

            if (File.Exists(filepath))
            {
                string[] jsonarray = File.ReadAllLines(filepath);
                string json = string.Join("", jsonarray);

                DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);

                DataTable dt = ds.Tables["maps"];

                //foreach (DataRow dr in dt.Rows)
                //{
                //    Console.WriteLine(dr["mapid"] + " - " + dr["mapper"] + " - " + dr["mapname"]);
                //}

                return dt;
            }
            else
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("mapid", typeof(int));
                dt.Columns.Add("mapper", typeof(string));
                dt.Columns.Add("mapname", typeof(string));
                dt.Rows.Add(-1, "-1", "-1");
                return dt;
            }
        }

        static string ReadApiLeaderboardToJson(string mapname, int mapid, int page)
        {
            using (var w = new WebClient())
            {
                var json_data = string.Empty;

                try
                {
                    string realname = mapname.Replace(" ", "-");
                    int realpage = page * 10;
                    json_data = w.DownloadString(@"http://" + @"df.hitboxteam.com/backend6/scores.php?level=" + realname + @"-" + mapid + @"&offset=" + realpage + @"&max=10");
                }
                catch (Exception) { }

                return json_data;
            }
        }

        static DataTable ReadApiLeaderboardToDt(string json)
        {
            DataSet ds = JsonConvert.DeserializeObject<DataSet>(json);

            DataTable dt = ds.Tables["scorelist"];

            return dt;
        }


        static bool CheckSS(string racer, int cmrid)
        {
            DataTable maps;
            maps = UpdateJsonToDtMaps(cmrid).Copy();

            bool res = false;

            int mapsCount = maps.Rows.Count;

            string[] score = new string[mapsCount - 1];

            for (int i = 0; i < mapsCount; i++)
            {
                DataTable scores;
                scores = ReadApiLeaderboardToDt(ReadApiLeaderboardToJson(maps.Rows[i]["mapname"].ToString(), Convert.ToInt32(maps.Rows[i]["mapid"]), 0)).Copy();
            }

            return res;
        }

        static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }
    } /* IRCBot */
}