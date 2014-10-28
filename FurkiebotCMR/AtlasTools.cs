using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;


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
    /// </summary>
    public struct LeaderboardEntry {
    }


    /// <summary>
    /// Class containing static Atlas Tools methods.
    /// </summary>
    public class Atlas {
        static string RECENT_MAPS_URL = @"http://df.hitboxteam.com/backend6/maps.php?sort=new&offset=0&max=30";


        /// <summary>
        /// Gets the recent map list from atlas.
        /// </summary>
        /// <returns>A list of AtlasMapResult structs.</returns>
        public static List<AtlasMapResult> GetRecentMapList() {
            string textFromFile = (new WebClient()).DownloadString(RECENT_MAPS_URL);

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
}
