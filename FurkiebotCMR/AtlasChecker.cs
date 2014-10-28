﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AtlasTools;

namespace FurkiebotCMR {
    public delegate void NotifyCallback(List<AtlasMapResult> updatedList);

    class AtlasChecker {
        public const int CHECK_EVERY_SECONDS = 10;
        public const string INSTALL_URL = @"http://eklipz.us.to/cmr/install.php";
        public const string ATLAS_MAP_URL = @"http://atlas.dustforce.com/";

        private readonly object _checkingLock = new Object();

        private FurkieBot furkiebot;
        private Thread checkerThread;
        private int uploadedCount;
        private bool exit = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="AtlasChecker"/> class.
        /// </summary>
        /// <param name="fb">A reference to FurkieBot.</param>
        public AtlasChecker(FurkieBot fb) {
            furkiebot = fb;
            checkerThread = null;
            uploadedCount = 0;
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
                    List<AtlasMapResult> maps = Atlas.GetRecentMapList();

                    lock (furkiebot._updatingMapsLock) {
                        Dictionary<string, MapData> curMaps = furkiebot.GetMaps();
                        bool altered = false;
                        foreach (AtlasMapResult result in maps) {
                            if (curMaps.ContainsKey(result.clean_name) && (result.id != curMaps[result.clean_name].id)) { //If the atlas map is in our list of maps, AND it hasn't yet been id'd....
                                MapData temp = curMaps[result.clean_name];
                                temp.id = result.id;
                                if (curMaps[result.clean_name].id <= 0) {   //Newly uploaded map
                                    uploadedCount++;
                                    furkiebot.MessageRacechan("CMR map #" + uploadedCount + " uploaded to Atlas: " + ATLAS_MAP_URL + result.id + FurkieBot.SEP + @"Install: " + GetMapInstallUrl(result.id, result.urlName) + FurkieBot.SEP + (furkiebot.AcceptedCount - uploadedCount) + " left to be uploaded!");
                                } else {                                    //reuploaded map
                                    furkiebot.MessageRacechan("CMR map REUPLOADED to Atlas: " + ATLAS_MAP_URL + result.id + FurkieBot.SEP + @"Install: " + GetMapInstallUrl(result.id, result.urlName) + FurkieBot.SEP + (furkiebot.AcceptedCount - uploadedCount) + " left to be uploaded!");
                                }
                                curMaps[result.clean_name] = temp;
                                altered = true;
                            }
                        }

                        if (altered) {
                            furkiebot.SetMaps(curMaps);
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