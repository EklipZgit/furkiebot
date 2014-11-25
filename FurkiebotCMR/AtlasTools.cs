/**
 * AtlasTools.cs
 * Utilities for the Dustforce Atlas. Contains several utility classes, read each for their
 * respective use details.
 * @author Travis Drake
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using MapCMR;

namespace AtlasTools {
    /// <summary>
    /// Class for values returned by querying the recent atlas maps, as returned by the Hitbox API in JSON.
    /// </summary>
    public class AtlasMapResult {
        public string name;
        public string urlName;
        public string clean_name;
        public int id;
    }


	/// <summary>
	/// Class representing all of the information obtainable by querying a specific maps atlas page.
	/// todo not done.
	/// </summary>
	public class AtlasMap {

	}


    /// <summary>
	/// A class representing Dustforce LeaderboardEntry's as returned by the Hitbox API in JSON.
    /// </summary>
	public class LeaderboardEntry {
		public const int DUSTMAN = 0;
		public const int DUSTGIRL = 1;
		public const int DUSTWORTH = 2;
		public const int DUSTKID = 3;
		
		//{"rank":1,"name":"ShurykaN","user_id":"109761","steam_id":"76561198047089919","character":"1","score":"1285",
		//"score_finesse":"S","score_thoroughness":"S","time":"33799","timestamp":"1416701791","replay":"3959401"}
		public long rank;
		public string name;
		public long user_id;
		public long steam_id;
		public byte character;
		public int score;
		public char score_finesse;
		public char score_thoroughness;
		public int time;
		public long timestamp;
		public long replay;
    }

	public class LeaderboardResult {
		public string clean_name;
		public int total_count;
		public LeaderboardEntry[] best_scores;
		public LeaderboardEntry[] best_times;
	}


    /// <summary>
    /// Class containing static Atlas Tools methods.
    /// </summary>
	public class Atlas {
		/// <summary>
		/// Gets the recent maps URL.
		/// </summary>
		/// <param name="count">The number of results to retrieve.</param>
		/// <param name="start">The initial offset to start retrieving new maps at.</param>
		/// <returns>A string URL to retrieve the desired results.</returns>
		private static string GetRecentMapsUrl(int count, int start = 0) {
			return @"http://df.hitboxteam.com/backend6/maps.php?sort=new&offset=" + start + "&max=" + count;
		}


        /// <summary>
        /// Gets the recent map list from atlas.
        /// </summary>
        /// <returns>A list of AtlasMapResult structs.</returns>
        public static List<AtlasMapResult> GetRecentMapList(int num = 30) {
            string textFromFile = (new WebClient()).DownloadString(GetRecentMapsUrl(num));

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
	/// LeaderboardFetcher class, used to retrieve leaderboard entries for maps.
	/// @author Travis Drake
	/// </summary>
	public class LeaderboardFetcher : IDisposable {
		private WebClient webClient;
		private const int RETRY_LIMIT = 5;

		/// <summary>
		/// Indexer to get a <see cref="LeaderboardResult" />.
		/// </summary>
		/// <param name="map">The map whose LeaderboardResult to retrieve.</param>
		/// <returns>
		/// null if the map has an invalid AtlasID, else a deserialized leaderboard.
		/// </returns>
		public LeaderboardResult this[CmrMap map] {
			get {
				if (map.AtlasID > 0) {
					return GetLeaderboard(map.Name, map.AtlasID);
				} else {
					return null;
				}
			}
		}


		/// <summary>
		/// Initializes a new instance of the <see cref="LeaderboardFetcher"/> class.
		/// Just Initializes the webClient. Make sure this is properly disposed of!
		/// </summary>
		public LeaderboardFetcher() {
			webClient = new WebClient();
		}


		/// <summary>
		/// Gets the leaderboard for the provided mapname and ID.
		/// </summary>
		/// <param name="mapname">The mapname.</param>
		/// <param name="mapAtlasID">The map atlas identifier.</param>
		/// <returns>A <see cref="LeaderboardResult" /> for the provided map and mapAtlasID.</returns>
		/// <exception cref="System.Exception">
		/// Invalid mapname and ID? Atlas backend returned an error.
		/// or
		/// Could not successfully retrieve the leaderboard page. Max retry attempts exceeded. (could be on atlas's end).
		/// or
		/// mapAtlasID invalid. mapAtlasID must be greater than 0 and within a valid range.
		/// </exception>
		private LeaderboardResult GetLeaderboard(string mapname, int mapAtlasID) {
			if (mapAtlasID > 0) {
				string json_data = string.Empty;
				bool success = false;
				int attempts = 0;
				while (!success && attempts < RETRY_LIMIT) {
					try {
						attempts++;
						json_data = webClient.DownloadString(GetLeaderboardUrl(mapname, mapAtlasID));
						success = true;
					} catch (Exception) { }
				}
				if (success) {
					if (json_data.StartsWith("{ error:")) {
						throw new Exception("Invalid mapname and ID? Atlas backend returned an error.");
					}
					return JsonConvert.DeserializeObject<LeaderboardResult>(json_data);
				} else {
					throw new Exception("Could not successfully retrieve the leaderboard page. Max retry attempts exceeded.");
				}
			} else {
				throw new Exception("mapAtlasID invalid: " + mapAtlasID + ". It must be greater than 0 and within a valid range.");
			}
		}


		/// <summary>
		/// Gets the score leaderboard.
		/// </summary>
		/// <param name="mapname">The mapname.</param>
		/// <param name="mapAtlasID">The map atlas identifier.</param>
		/// <returns>The score leaderboard for the map.</returns>
		private LeaderboardEntry[] GetScoreLeaderboard(string mapname, int mapAtlasID) {
			return GetLeaderboard(mapname, mapAtlasID).best_scores;
		}


		/// <summary>
		/// Gets the time leaderboard.
		/// </summary>
		/// <param name="mapname">The mapname.</param>
		/// <param name="mapAtlasID">The map atlas identifier.</param>
		/// <returns>The time leaderboard for the map.</returns>
		private LeaderboardEntry[] GetTimeLeaderboard(string mapname, int mapAtlasID) {
			return GetLeaderboard(mapname, mapAtlasID).best_times;
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

	/// <summary>
	/// A class to retrieve additional information about a map. Useful for things like parsing info about random maps, etc.
	/// Parses information from the HTML in a custom map's Atlas page.
	/// TODO not implemented....
	/// </summary>
	public class MapFetcher : IDisposable {
		private WebClient webClient;

		public MapFetcher() {
			webClient = new WebClient();
		}

		public string GetAtlasMapUrl(int mapid) {
			return "lolfuck" + mapid;
		}

		public void Dispose() {
			webClient.Dispose();
		}
	}
}
