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

    public class BrainzArtist
    {
        public string Artist { get; set; }
        public string MBID { get; set; }
        public int Score { get; set; }
        public BrainzAlbum[] Albums { get; set; }
    }

    public class BrainzTrack
    {
        public string Title { get; set; }
        public string MBID { get; set; }
        public double Score { get; set; }
        public int Disc { get; set; }
        public int Position { get; set; }
        public string Number { get; set; }
        public int Length { get; set; }
        public string[] AlternateTitles { get; set; }
    }
}
