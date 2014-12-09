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
using System.Web;

namespace AtlasTools {
    /// <summary>
    /// Class for values returned by querying the recent atlas maps, as returned by the Hitbox API in JSON.
    /// </summary>
    public class AtlasRecentMapResult {
        public string name;
        public string urlName;
		/// <summary>
		/// The clean_name from hitboxes API, no idea what the difference is between name and clean_name.
		/// </summary>
        public string clean_name;
        public int id;
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
		/// <param name="num">The number of recent maps to return. Default 30.</param>
		/// <returns>
		/// A list of AtlasMapResult structs.
		/// </returns>
        public static List<AtlasRecentMapResult> GetRecentMapList(int num = 30) {
            string textFromFile = (new WebClient()).DownloadString(GetRecentMapsUrl(num));

            List<AtlasRecentMapResult> preResult = JsonConvert.DeserializeObject<List<AtlasRecentMapResult>>(textFromFile);
            List<AtlasRecentMapResult> results = new List<AtlasRecentMapResult>();
            foreach (AtlasRecentMapResult map in preResult) {
                AtlasRecentMapResult toAdd = new AtlasRecentMapResult();
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
	/// Class representing all of the information obtainable by querying a specific maps atlas page.
	/// TODO not done.
	/// </summary>
	public class AtlasMap {
		public string Name { get; set; }
		public string Author { get; set; }
		public DateTime TimeStamp { get; set; }
        public TimeSpan Age {
            get {
                return DateTime.UtcNow - TimeStamp;
            }
        }
		public string[] Tags { get; set; }
		public string Description { get; set; }
		public double Rating { get; set; }
		public int RatingVotes { get; set; }
		public double Difficulty { get; set; }
		public int DifficultyVotes { get; set; }
		

	}


	public class AtlasComment {
		#region EXAMPLE
		/**
<div class="qa-a-list-item hentry answer" id="aCOMMENT_ID">
	<form method="POST" action="../ATLAS_ID/outflow">
		<div class="qa-a-item-main">
			<div class="comment-left">
				<div class="avatar-comment pull-left centered">
					------- Avatar image
					<span class="qa-a-item-avatar">
						<a href="../user/ShurykaN" class="qa-avatar-link"><img src="../?qa=image&amp;qa_blobid=2532377085645455136&amp;qa_size=60" width="60" height="60" class="qa-avatar-image"></a>
					</span>
				</div>
				<div class="pull-left muted comment-poster-info">
					------- Author and Timestamp
					<div class="comment-poster-content">
						<strong><span class="vcard author"><a href="../user/ShurykaN" class="qa-user-link url nickname">ShurykaN</a></span></strong>
						<br>said <span class="published"><span class="value-title" title="2014-12-01T23:30:49+0000"></span>1 day</span> ago
					</div>
				</div>
				<div class="comment-cap"></div>
				<div style="clear:both;"></div>
			</div>
			<div class="comment-right well">
				<div class="qa-a-item-content">
					<a name="COMMENT_ID"></a><span class="entry-content">Byronyello your maps are always a joy to play!</span>
				</div>
				<div class="qa-a-item-buttons">
					<input name="aCOMMENT_ID_doflag" onclick="return qa_answer_click(COMMENT_ID, 17158, this);" value="flag" title="flag this comment as spam or inappropriate" type="submit" class="button-links super-muted">
					<input name="aCOMMENT_ID_docomment" onclick="return qa_toggle_element('cCOMMENT_ID')" value="reply" title="reply directly to this comment" type="submit" class="button-links super-muted">
				</div>
			</div>
									
			<div class="qa-a-item-c-list" style="display:none;" id="cCOMMENT_ID_list">
			</div> <!-- END qa-c-list -->							
		</div>
	</form> <!-- END qa-a-item-main -->
	<div class="qa-a-item-clear">
	</div>
</div>
		 */
		#endregion

		public string Author { get; set; }
		public string Content { get; set; } //<div class="qa-a-item-content"><a name="17159"></a><span class="entry-content">CONTENT</span>
		public int CommentId { get; set; } //(SEE CONTENT) <a name="17159"></a>
											
		public DateTime TimeStamp { get; set; }
        public TimeSpan Age {
            get {
                return DateTime.UtcNow - TimeStamp;
            }
        }

		public string AuthorImage { get; set; }//  "/?qa=image&qa_blobid=2532377085645455136&qa_size=60"  //THE FUCK IS THIS SHIT?
		
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

		public AtlasMap FetchMapInfo(int atlasId) {
			string atlasHtml = webClient.DownloadString(GetAtlasMapUrl(atlasId));
			//if (isValidMap) { } //TODO figure out how to check if its a valid map result.
			AtlasMap theMap = new AtlasMap();
			//BufferedReader
			//breakdown of search process....
			/**
<div class="map paper map-page-main">
	<div class="map-header">
		<div class="map-info">
			<h1><a class="dark-link" href="../4439/outflow">Outflow</a></h1>
			<div class="map-info-stats">
				<div class="map-info-stats-left">
					<div class="name-area">
						<div class="pull-left avatar-main">
							<span class="qa-q-view-avatar">
								<a href="../user/Byronyello" class="qa-avatar-link"><img src="../?qa=image&amp;qa_blobid=10666441705912608421&amp;qa_size=32" width="32" height="32" class="qa-avatar-image"></a>
							</span>
						</div>
						<div class="pull-left">
							<span class="muted">by </span><strong><a href="http://atlas.dustforce.com/user/Byronyello">Byronyello</a></strong>
							<br><span class="muted">created 1 day ago</span>
						</div>
						<div style="clear:both;"></div>
					</div>
					<a href="http://dustworth.com/level/Outflow-4439"><b>Leaderboards</b></a><br>
					<span class="muted">79 views | 
						95 downloads							
					</span>
					<br>						
				</div>
				<div class="map-info-stats-right">
					<p>
						<span class="subtle-header">Rating</span>
						<br>
						<span id="vote_r4439" class="rating-slider rating-quality pointer  score-4.25 state-3 user-vote-0 vote-count-16 total-68" title="Score: 4.25">
							<i class="rater rating-1 icon-heart"></i>
							<i class="rater rating-2 icon-heart"></i>
							<i class="rater rating-3 icon-heart"></i>
							<i class="rater rating-4 icon-heart"></i>
							<i class="rater rating-5 icon-heart heart-dull"></i>
						</span>
						<span class="muted vote-count-label">/ 16 votes </span>
					</p>
					<p>
						<span class="subtle-header">Difficulty</span>
						<br>
						<span id="vote_d4439" class="rating-slider rating-difficulty pointer  score-2.875 state-3 user-vote-0 vote-count-8 total-23" title="Score: 2.88">
							<i class="rater rating-1 icon-fire"></i>
							<i class="rater rating-2 icon-fire"></i>
							<i class="rater rating-3 icon-fire"></i>
							<i class="rater rating-4 icon-fire fire-dull"></i>
							<i class="rater rating-5 icon-fire fire-dull"></i>
						</span>
						<span class="muted vote-count-label">/ 8 votes </span>
					</p>
				</div>
			</div>
			<div class="tag-area">
				<div class="subtle-header">Tags</div>
				<a title="find more maps with the tag 'underground'" href="../tag/underground"><span class="label label-normal">underground</span></a>
				<a title="find more maps with the tag 'short'" href="../tag/short"><span class="label label-short">short</span></a>
				<a title="find more maps with the tag 'city'" href="../tag/city"><span class="label label-city">city</span></a>
				<a title="find more maps with the tag 'forest'" href="../tag/forest"><span class="label label-forest">forest</span></a>
				<a title="find more maps with the tag 'medium difficulty'" href="../tag/medium-difficulty"><span class="label label-normal">medium difficulty</span></a>
			</div>
		</div>
		<div class="map-image-holder">
			<div id="outflow" class="map-image" style="background-image:url('http://atlas.dustforce.com/gi/maps/Outflow-4439.png')">					
			</div>
		</div>
	</div>
	<hr>
	<div class="map-body">
		<div class="map-sidebar">
			<div class="centered well map-installer">
				<h1 class="map-installer-header compact">get this map <abbr class="map-help muted" title="If you have Dustforce installed, the map will automatically be downloaded and installed to the map directory.">?</abbr></h1>
				<div class="install-button-area">
					<a href="dustforce://installPlay/4439/Outflow" class="btn btn-primary btn-large btn-installer" title="click to launch Dustforce and play this map">
						<i class="icon icon-white icon-share-alt"></i> install and play
					</a>
					<div class="button-seperator-break compact"><br></div>
					<a href="dustforce://install/4439/Outflow" class="btn btn-primary btn-large btn-installer" title="click to download and install this map in the background">
						<i class="icon icon-white icon-download nudge-down"></i> install only
					</a>
				</div>
				<span class="muted">
					<a href="http://atlas.dustforce.com/gi/downloader.php?id=4439" title="Put this file in your 'dustforce/user/levels/' directory">direct file download</a>
				</span>
			</div>
			<div class="share-area centered">
				<a class="custom-button custom-button-twitter" href="javascript:popup('http://twitter.com/intent/tweet?url=http%3A%2F%2Fatlas.dustforce.com%2F4439%2Foutflow&text=check%20out%20this%20Dustforce%20map', 'twitter','600','300');" title="Tweet this map"></a>
				<a class="custom-button custom-button-facebook" href="javascript:popup('http://www.facebook.com/sharer.php?u=http%3A%2F%2Fatlas.dustforce.com%2F4439%2Foutflow&t=check%20out%20this%20Dustforce%20map', 'facebook','670','350');" title="Share this map on Facebook"></a>
				<a class="custom-button custom-button-google" href="javascript:popup('https://plusone.google.com/_/+1/confirm?hl=en&url=http%3A%2F%2Fatlas.dustforce.com%2F4439%2Foutflow', 'google','445','195');" title="Share this map on Google Plus"></a>
			</div>
		</div>
		<div class="map-content">
			<div class="map-description">
				<div class="subtle-header">map notes</div>
				<div class="map-description-contents">
					<a name="17158"></a><span class="entry-content">My 14th map. It's taken a while to complete this thing, I can tell you that. I had finalised the gameplay around one and a half months ago (indeed, I started work on this map before <a href="http://atlas.dustforce.com/4314/abandonware" target="_blank"><strong>Abandonware</strong></a> had released). However, I had not then finished the visuals of the map, and somehow <strong>Outflow</strong> has managed to take out so much time from me to just get it to look how I wanted. Thankfully, I am now finished - though I do indeed have a new map in the pipeline, so there's that to look forward to. Finally, as always, there's a not-very-well-hidden apple that is just waiting to be unceremoniously found!</span>
				</div>
				<div class="qa-q-view hentry question" id="q17158">
					<form method="POST" action="../4439/outflow">
						<div class="qa-q-view-main">
							<div class="qa-q-view-buttons">
								<input name="q_doflag" value="flag" title="flag this map as spam or inappropriate" type="submit" class="button-links super-muted">
									<input type="hidden" name="qa_click" value="">
														
													
							</div>
											
						<div class="qa-c-form">
						</div> <!-- END qa-c-form -->

					</div></form> <!-- END qa-q-view-main -->
					<div class="qa-q-view-clear">
					</div>
				</div> <!-- END qa-q-view -->

			</div>
		</div>
		<div style="clear:both;"></div>
	</div>
</div>
			*/
            return new AtlasMap();
		}

		public string GetAtlasMapUrl(int mapid) {
			return "http://atlas.dustforce.com/" + mapid;
		}

		public void Dispose() {
			webClient.Dispose();
		}
	}
}
