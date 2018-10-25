namespace Soulseek.NET
{
    using Soulseek.NET.Messaging.Responses;
    using System.Collections.Generic;

    public class SearchResult
    {
        public string SearchText { get; internal set; }
        public int Ticket { get; internal set; }
        public IEnumerable<SearchResponse> Results { get; internal set; }
        public bool Cancelled { get; internal set; }
    }
}
