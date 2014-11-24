using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using MapCMR;

namespace AtlasTools {
    /// <summary>
    /// Struct for values returned by querying the recent atlas maps.
    /// </summary>
    public struct AtlasMapResult {
        public string name;
        public string urlName;
        public string clean_name;
        public int id;
    }


    /// <summary>
    /// Nothing, yet. Will be a struct for leaderboard entries when I get bored and implement this stuff.
	/// TODO
    /// </summary>
    public struct LeaderboardEntry {
    }


    /// <summary>
    /// Class containing static Atlas Tools methods.
    /// </summary>
    public class Atlas {
        static string RECENT_MAPS_URL = @"http://df.hitboxteam.com/backend6/maps.php?sort=new&offset=0&max=";


        /// <summary>
        /// Gets the recent map list from atlas.
        /// </summary>
        /// <returns>A list of AtlasMapResult structs.</returns>
        public static List<AtlasMapResult> GetRecentMapList(int num = 30) {
            string textFromFile = (new WebClient()).DownloadString(RECENT_MAPS_URL + num);

            List<AtlasMapResult> preResult = JsonConvert.DeserializeObject<List<AtlasMapResult>>(textFromFile);
            List<AtlasMapResult> results = new List<AtlasMapResult>();
            foreach (AtlasMapResult map in preResult) {
                AtlasMapResult toAdd;
                string mapName = map.name;
                int lastIndex = mapName.LastIndexOf('-');
                toAdd.id = int.Parse(mapName.Substring(lastIndex + 1, mapName.Length - lastIndex - 1));
                toAdd.name = mapName;
                toAdd.urlName = mapName.Substring(0, lastIndex);
                toAdd.clean_name = map.clean_name;
                results.Add(toAdd);
            }

            return results;
        }


        ////TEST GetRecentMapList()
        //public static void Main(string[] args) {
        //    foreach (AtlasMapResult map in GetRecentMapList()) {
        //        Console.WriteLine("name: " + map.name);
        //        Console.WriteLine("clean_name: " + map.clean_name);
        //        Console.WriteLine("id: " + map.id);
        //        Console.WriteLine("urlName: " + map.urlName);
        //        Console.WriteLine();
        //    }
        //    Console.ReadLine();
        //}
    }


	/// <summary>
	/// Not yet fully implemented, TODO check up on how leaderboard entries are returned from th
	/// the Atlas Server.
	/// @author Travis Drake
	/// </summary>
	public class LeaderboardFetcher : IDisposable {
		private WebClient webClient;

		public List<LeaderboardEntry> this[CmrMap map] {
			get {
				return GetLeaderboard(map.Name, map.AtlasID);
			}
		}


		public LeaderboardFetcher() {
			webClient = new WebClient();
		}


		private List<LeaderboardEntry> GetLeaderboard(string mapname, int mapAtlasID) {
			string json_data = string.Empty;

			try {
				json_data = webClient.DownloadString(GetLeaderboardUrl(mapname, mapAtlasID));
			} catch (Exception) { }

			return JsonConvert.DeserializeObject<List<LeaderboardEntry>>(json_data);
		}


		private static string GetLeaderboardUrl(string mapname, int mapAtlasID) {
			string realname = mapname.Replace(" ", "-").Trim();
			return @"http://df.hitboxteam.com/backend6/scores.php?level=" + realname + "-" + mapAtlasID + "&offset=0&max=100";
		}



		public void Dispose() {
			webClient.Dispose();
		}
	}
}
