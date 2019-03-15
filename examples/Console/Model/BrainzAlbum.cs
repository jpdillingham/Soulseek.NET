using System;
using System.Collections.Generic;
using System.Text;

namespace Console.Model
{
    public class BrainzAlbum
    {
        public string Title { get; set; }
        public string MBID { get; set; }
        public double Score { get; set; }
        public BrainzTrack[] Tracks { get; set; }
    }
}
