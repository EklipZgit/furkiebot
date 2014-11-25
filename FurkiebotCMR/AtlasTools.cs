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
	/// TODO need this done for SS checking........
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
                AtlasMapResult toAdd = new AtlasMapResult();
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

		/// <summary>
		/// Indexer to get a <see cref="List{LeaderboardEntry}" /> of <see cref="LeaderboardEntry" />'s.
		/// </summary>
		/// <value>
		/// The <see cref="List{LeaderboardEntry}" />.
		/// </value>
		/// <param name="map">The map whose leaderboard entries to retrieve.</param>
		/// <returns></returns>
		public List<LeaderboardEntry> this[CmrMap map] {
			get {
				return GetLeaderboard(map.Name, map.AtlasID);
			}
		}


		public LeaderboardFetcher() {
			webClient = new WebClient();
		}


		private List<LeaderboardEntry> GetLeaderboard(string mapname, int mapAtlasID) {
			//TODO somethings wrong with this, I'm not sure the JSON has just a list including both types of results.
			int LIMIT = 5;
			string json_data = string.Empty;
			bool success = false;
			int attempts = 0;
			while (!success && attempts < LIMIT) {
				try {
					attempts++;
					json_data = webClient.DownloadString(GetLeaderboardUrl(mapname, mapAtlasID));
					success = true;
				} catch (Exception) { }
			}
			if (success) {
				return JsonConvert.DeserializeObject<List<LeaderboardEntry>>(json_data);
			} else {
				throw new Exception("Could not successfully retrieve the leaderboard page. Max retry attempts exceeded.");
			}
		}


		private List<LeaderboardEntry> GetScoreLeaderboard(string mapname, int mapAtlasID) {
			//TODO figure out leaderboard formatting when I have internet access. Or save a page.....
			throw new NotImplementedException();
			return GetLeaderboard(mapname, mapAtlasID);
		}


		private List<LeaderboardEntry> GetTimeLeaderboard(string mapname, int mapAtlasID) {
			//TODO figure out leaderboard formatting when I have internet access. Or save a page.....
			throw new NotImplementedException();
			return GetLeaderboard(mapname, mapAtlasID);
		}


		/// <summary>
		/// Gets the leaderboard URL formatted properly given the map name, map ID, and optional entry offset and result count.
		/// </summary>
		/// <param name="mapname">The maps name.</param>
		/// <param name="mapAtlasID">The maps atlas ID number.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="count">The count.</param>
		/// <returns>The formatted URL to be retrieved.</returns>
		private static string GetLeaderboardUrl(string mapname, int mapAtlasID, int offset = 0, int count = 100) {
			string realname = mapname.Replace(" ", "-").Trim();
			return @"http://df.hitboxteam.com/backend6/scores.php?level=" + realname + "-" + mapAtlasID + "&offset=" + offset + "&max=" + count;
		}



		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// ^ yeah that
		/// </summary>
		public void Dispose() {
			webClient.Dispose();
		}
	}
}
