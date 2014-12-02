/**
 * AtlasChecker.cs
 * Threaded checker class used to monitor map uploads on atlas, to automatically identify when accepted maps
 * have been published.
 * @Author Travis Drake
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AtlasTools;
using MapCMR;

namespace FurkiebotCMR {
    public delegate void NotifyCallback(List<AtlasMapResult> updatedList);

    class AtlasChecker {
        public const int CHECK_EVERY_SECONDS = 5;
        public const string INSTALL_URL = @"http://eklipz.us.to/cmr/install.php";
        public const string ATLAS_MAP_URL = @"http://atlas.dustforce.com/";

        private readonly object _checkingLock = new Object();

        private FurkieBot furkiebot;
        private MapManager MapMan;
        private Thread checkerThread;
        private int uploadedCount;
        private bool exit = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AtlasChecker"/> class.
        /// </summary>
        /// <param name="fb">A reference to FurkieBot.</param>
        public AtlasChecker() {
            furkiebot = FurkieBot.Instance;
            checkerThread = null;
            uploadedCount = 0;
            MapMan = MapManager.Instance;
        }


        /// <summary>
        /// Tells the AtlasChecker to start the Atlas Checking thread.
        /// </summary>
        public void StartChecking() {
            if (checkerThread == null || (!checkerThread.IsAlive)) {
                uploadedCount = 0;
                checkerThread = new Thread(() => Check());
                checkerThread.Start();
            }
        }

        /// <summary>
        /// Stops the Atlas Checking thread.
        /// </summary>
        public void StopChecking() {
            if (checkerThread.IsAlive) {
                lock (_checkingLock) {  //Ensure we only terminate while the thread is intentionally asleep, not while it is executing.
                    exit = true;
                    //checkerThread.Abort();  //Should terminate the thread ungracefully, (to avoid unjoined threads b/c idk?), but locking ensures that it happens during the sleep.
                }
            }
        }
        

        /// <summary>
        /// The function to be run in a new thread, which checks atlas every interval for new maps matching the maps set to be uploaded.
        /// This is used to allow FurkieBot to monitor atlas uploads while still responding normally in IRC.
        /// </summary>
        private void Check() {
            while (!exit) {
                lock (_checkingLock) {  //This lock ensures that when stopChecking is locked (so, while this is not sleeping) the thread will not be terminated.
                                        //Thus ensuring that this / furkiebot will not be left in an undefined state.
                    List<AtlasMapResult> atlasMaps = Atlas.GetRecentMapList();

                    foreach (AtlasMapResult result in atlasMaps) {
                        string name = result.clean_name.Trim().ToLower();
                        CmrMap map = MapMan[name];
                        if (map != null && map.Accepted == true && map.IsAtlasIdForced == false && (result.id != map.AtlasID)) { 
							//If the atlas map is in our list of maps, AND is an accepted map, AND not forced, AND it hasn't yet been id'd....
                            
                            map.AtlasID = result.id;
                            map.Name = result.clean_name;
                            if (MapMan[name].AtlasID <= 0) {   //Newly uploaded map
                                uploadedCount++;
                                furkiebot.MessageRacechan(FurkieBot.FormatNumber(uploadedCount) + " map uploaded to Atlas, " + (furkiebot.AcceptedCount - uploadedCount) + " left to be uploaded!" + FurkieBot.SEP + @"Install: " + GetMapInstallUrl(result.id, result.urlName) + FurkieBot.SEP + ATLAS_MAP_URL + result.id);
                            } else {                           //reuploaded map
                                furkiebot.MessageRacechan("CMR map REUPLOADED to Atlas, " + (furkiebot.AcceptedCount - uploadedCount) + " left to be uploaded!" + FurkieBot.SEP + @"Install: " + GetMapInstallUrl(result.id, result.urlName) + FurkieBot.SEP + "DELETE THE OLD ONE FROM YOUR CUSTOM MAPS DIRECTORY" + FurkieBot.SEP + ATLAS_MAP_URL + result.id);
                            }
                            MapMan.SaveMap(map);
                        }
                    }
                }
                Thread.Sleep(CHECK_EVERY_SECONDS * 1000);
            }
        }

        /// <summary>
        /// Gets the map install URL formatted properly.
        /// </summary>
        /// <param name="mapID">The map ID number.</param>
        /// <param name="mapUrlName">Name of the map, URL formatted.</param>
        /// <returns>The install URL</returns>
        public string GetMapInstallUrl(int mapID, string mapUrlName) {
            return INSTALL_URL + "?id=" + mapID + "&name=" + mapUrlName;
        }
    }
}
