using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReplayLibrary
{
    public class Header
    {
        public int? unk { get; set; }
        public int? uncompressedSize { get; set; }
        public int frames { get; set; }
        public int? character { get; set; }
        public string level { get; set; }
    }

    public class EntityFrameContainer
    {
        public int unk1 { get; set; }
        public int unk2 { get; set; }
        public List<EntityFrame> entityFrames { get; set; }
    }

    public class Meta
    {
        public string user { get; set; }
        public string level { get; set; }
        public string time { get; set; }
        public string character { get; set; }
        public string score_completion { get; set; }
        public string score_finesse { get; set; }
        public string timestamp { get; set; }
        public string replay_id { get; set; }
        public int validated { get; set; }
        public int rank_all_score { get; set; }
        public int rank_all_time { get; set; }
        public int rank_char_score { get; set; }
        public int rank_char_time { get; set; }
        public string username { get; set; }
        public bool dustkid { get; set; }
        public string levelname { get; set; }
    }

    public class RawJsonValues
    {
        public Header header { get; set; }
        public List<string> inputs { get; set; }
        public List<EntityFrameContainer> entityFrameContainers { get; set; }
        public string username { get; set; }
        public Meta meta { get; set; }
    }

    public class EntityFrame
    {
        public int time { get; set; }
        public long xpos { get; set; }
        public long ypos { get; set; }
        public int xspeed { get; set; }
        public int yspeed { get; set; }

    }
}
