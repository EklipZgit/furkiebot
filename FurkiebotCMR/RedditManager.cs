﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using RedditSharp;

namespace FurkiebotCMR
{
    public class RedditManager {
        private FurkieBot furkiebot;
        private Reddit reddit = new Reddit();
        private List<String> postIds = new List<String>();

        public RedditManager(string username, string password) {
            HasLoggedIn(username, password);
            GenerateList();
        }
        
        private bool HasLoggedIn(string username, string password) {
            try {
                var user = reddit.LogIn(username, password);
                Console.WriteLine("We did it Reddit!");
                return true;
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        private void GenerateList() {
            var subreddit = reddit.GetSubreddit("/r/dustforce");
            foreach (var post in subreddit.New.Take(25)) {
                postIds.Add(post.Id);
            }
            Console.WriteLine("List of Reddit posts generated.");
        }

        public void CheckForNewPosts() {
            while (true) {
                if (furkiebot == null) {
                    furkiebot = FurkieBot.Instance;
                }
                var subreddit = reddit.GetSubreddit("/r/dustforce");
                foreach (var post in subreddit.New.Take(5)) {
                    if (!postIds.Contains(post.Id)) {
                        furkiebot.Msg("#dustforcee", "New Reddit post: \"" + post.Title + "\" by " + post.Author + " - " + post.Shortlink);

                        postIds.Add(post.Id);
                    }
                }
                Thread.Sleep(30000);
            }
        }
    }
}
