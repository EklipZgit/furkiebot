using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;


namespace TsamTools {
    /// <summary>
    /// Struct for values returned by querying leaderboard records
    /// </summary>
    public struct TsamLeaderboardResult {
        public string name;
        public string urlName;
        public string clean_name;
        public int id;
    }

    /// <summary>
    /// Class containing static Tsam Tools methods.
    /// </summary>
    public class Atlas {
        static string RECENT_MAPS_URL = @"";


        /// <summary>
        /// ---
        /// </summary>
        /// <returns>A list of TsamLeaderboardResult structs. Lots to be changed and stuff</returns>
        public static List<TsamLeaderboardResult> GetRecentMapList()
        {
            string textFromFile = (new WebClient()).DownloadString(RECENT_MAPS_URL);

            List<TsamLeaderboardResult> preResult = JsonConvert.DeserializeObject<List<TsamLeaderboardResult>>(textFromFile);
            List<TsamLeaderboardResult> results = new List<TsamLeaderboardResult>();
            foreach (TsamLeaderboardResult map in preResult)
            {
                TsamLeaderboardResult toAdd;
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
