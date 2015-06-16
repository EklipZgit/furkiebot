using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ReplayLibrary
{
    public class DustkidAPI
    {
        public static RawReplayData GetReplayData(string id)
        {
            string text = (new WebClient()).DownloadString(@"http://dustkid.com/json/replay/" + id + @"/showinputs");
            return JsonConvert.DeserializeObject<RawReplayData>(text);
        }
    }
}
