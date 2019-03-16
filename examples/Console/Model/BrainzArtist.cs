using System;
using System.Collections.Generic;
using System.Text;

namespace Console.Model
{
    public class BrainzArtist
    {
        public string Artist { get; set; }
        public string MBID { get; set; }
        public int Score { get; set; }
        public BrainzAlbum[] Albums { get; set; }
    }
}
