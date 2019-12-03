namespace WebAPI
{
    using Soulseek;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public interface ISearchTracker
    {
        ConcurrentDictionary<string, Search> Searches { get; }
        void AddOrUpdate(SearchResponseReceivedEventArgs args);
        void AddOrUpdate(SearchStateChangedEventArgs args);
        void Clear();
    }

    public class SearchTracker : ISearchTracker
    {
        public ConcurrentDictionary<string, Search> Searches { get; private set; } = 
            new ConcurrentDictionary<string, Search>();

        public void Clear()
        {
            Searches.Clear();
        }

        public void AddOrUpdate(SearchResponseReceivedEventArgs args)
        {
            Searches.AddOrUpdate(args.Search.SearchText, args.Search, (searchText, search) => args.Search);
        }

        public void AddOrUpdate(SearchStateChangedEventArgs args)
        {
            Searches.AddOrUpdate(args.Search.SearchText, args.Search, (searchText, search) => args.Search);
        }
    }

    //public class SearchRecord
    //{
    //    public Search Search { get; set; }
    //    public List<SearchResponse> Responses { get; set; }

    //    public static SearchRecord FromEventArgs(SearchStateChangedEventArgs args)
    //    {
    //        return new SearchRecord()
    //        {
    //            Search = args.Search,
    //            Responses = new List<SearchResponse>()
    //        };
    //    }

    //    public static SearchRecord FromEventArgs(SearchResponseReceivedEventArgs args)
    //    {
    //        return new SearchRecord()
    //        {
    //            Search = args.Search,
    //            Responses = new List<SearchResponse>() { args.Response }
    //        };
    //    }
    //}
}
