using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Console.Model
{
    public class Alias
    {
        [JsonProperty("sort-name")]
        public string ShortName { get; set; }

        public string Name { get; set; }
        public string Locale { get; set; }
        public string Type { get; set; }
        public bool Primary { get; set; }

        [JsonProperty("begin-date")]
        public string BeginDate { get; set; }

        [JsonProperty("end-date")]
        public string EndDate { get; set; }
    }

    public class Area
    {
        public string ID { get; set; }
        public string Type { get; set; }

        [JsonProperty("type-id")]
        public string TypeID { get; set; }

        public string Name { get; set; }
        [JsonProperty("sort-name")]
        public string SortName { get; set; }

        [JsonProperty("life-span")]
        public Lifespan Lifespan { get; set; }
    }

    public class Lifespan
    {
        public string Begin { get; set; }
        public string End { get; set; }
        public bool Ended { get; set; }
    }

    public class CoverArtArchive
    {
        public bool Artwork { get; set; }
        public bool Front { get; set; }
        public int Count { get; set; }
        public bool Back { get; set; }
        public bool Darkened { get; set; }
    }

    public class TextRepresentation
    {
        public string Language { get; set; }
        public string Script { get; set; }
    }

    public class Tag
    {
        public int Count { get; set; }
        public string Name { get; set; }
    }

    public class ReleaseEvent
    {
        public string Date { get; set; }
        public Area Area { get; set; }
    }

    public class Recording
    {
        public string Disambiguation { get; set; }
        public string ID { get; set; }
        public string Title { get; set; }
        public bool Video { get; set; }
        public int Length { get; set; }
    }
}
