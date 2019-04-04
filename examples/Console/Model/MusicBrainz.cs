using Newtonsoft.Json;
using System.Collections.Generic;

namespace Console.Model
{
    public class Alias
    {
        [JsonProperty("begin-date")]
        public string BeginDate { get; set; }

        [JsonProperty("end-date")]
        public string EndDate { get; set; }

        public string Locale { get; set; }

        public string Name { get; set; }

        public bool Primary { get; set; }

        [JsonProperty("sort-name")]
        public string ShortName { get; set; }

        public string Type { get; set; }
    }

    public class Area
    {
        public string ID { get; set; }

        [JsonProperty("life-span")]
        public Lifespan Lifespan { get; set; }

        public string Name { get; set; }

        [JsonProperty("sort-name")]
        public string SortName { get; set; }

        public string Type { get; set; }

        [JsonProperty("type-id")]
        public string TypeID { get; set; }
    }

    public class Artist
    {
        public IEnumerable<Alias> Aliases { get; set; }
        public Area Area { get; set; }

        [JsonProperty("begin-area")]
        public Area BeginArea { get; set; }

        public string Country { get; set; }
        public string Disambugiation { get; set; }
        public string Gender { get; set; }
        public string ID { get; set; }

        [JsonProperty("life-span")]
        public Lifespan Lifespan { get; set; }

        public string Name { get; set; }
        public int Score { get; set; }
        public string SortName { get; set; }
        public IEnumerable<Tag> Tags { get; set; }
        public string Type { get; set; }

        [JsonProperty("type-id")]
        public string TypeID { get; set; }
    }

    public class CoverArtArchive
    {
        public bool Artwork { get; set; }
        public bool Back { get; set; }
        public int Count { get; set; }
        public bool Darkened { get; set; }
        public bool Front { get; set; }
    }

    public class Lifespan
    {
        public string Begin { get; set; }
        public string End { get; set; }
        public bool Ended { get; set; }
    }

    public class Media
    {
        public string Format { get; set; }

        [JsonProperty("format-id")]
        public string FormatID { get; set; }

        public int Position { get; set; }

        public string Title { get; set; }

        [JsonProperty("track-count")]
        public int TrackCount { get; set; }

        [JsonProperty("track-offset")]
        public int TrackOffset { get; set; }

        public IEnumerable<Track> Tracks { get; set; }
    }

    public class Recording
    {
        public string Disambiguation { get; set; }
        public string ID { get; set; }
        public int Length { get; set; }
        public string Title { get; set; }
        public bool Video { get; set; }
    }

    public class Release
    {
        [JsonProperty("packaging-id")]
        public string PackagingID { get; set; }
        public string Asin { get; set; }
        [JsonProperty("status-id")]
        public string StatusID { get; set; }
        public string Disambiguation { get; set; }
        public string Date { get; set; }
        public string Packaging { get; set; }
        public string Status { get; set; }
        [JsonProperty("release-events")]
        public IEnumerable<ReleaseEvent> ReleaseEvents { get; set; }
        [JsonProperty("cover-art-archive")]
        public CoverArtArchive CoverArtArchive { get; set; }
        [JsonProperty("text-representation")]
        public TextRepresentation TextRepresentation { get; set; }
        public string Quality { get; set; }
        public string Title { get; set; }
        public string Country { get; set; }
        public string ID { get; set; }
        public IEnumerable<Media> Media { get; set; }
        public string Barcode { get; set; }
        public double Score { get; set; }
    }

    public class ReleaseEvent
    {
        public Area Area { get; set; }
        public string Date { get; set; }
    }

    public class Tag
    {
        public int Count { get; set; }
        public string Name { get; set; }
    }

    public class TextRepresentation
    {
        public string Language { get; set; }
        public string Script { get; set; }
    }

    public class Track
    {
        [JsonProperty("alternate-titles")]
        public IEnumerable<string> AlternateTitles { get; set; }

        public string ID { get; set; }
        public int Length { get; set; }
        public string Number { get; set; }
        public int Position { get; set; }
        public Recording Recording { get; set; }
        public double Score { get; set; }
        public string Title { get; set; }
    }
}