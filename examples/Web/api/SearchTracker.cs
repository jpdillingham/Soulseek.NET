namespace WebAPI
{
    using Soulseek;
    using Soulseek.Messaging.Messages;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public interface ISearchTracker
    {
        ConcurrentDictionary<string, Search> Searches { get; }
        void AddOrUpdate(SearchResponseReceivedEventArgs args);
        void AddOrUpdate(SearchStateChangedEventArgs args);
    }

    public class SearchTracker : ISearchTracker
    {
        public ConcurrentDictionary<string, Search> Searches { get; private set; } = 
            new ConcurrentDictionary<string, Search>();

        public void AddOrUpdate(SearchResponseReceivedEventArgs args)
        {
            Searches.AddOrUpdate(args.SearchText, new Search(args), (searchText, search) => 
            {
                search.Responses.Add(args.Response);
                return search;
            });
        }

        public void AddOrUpdate(SearchStateChangedEventArgs args)
        {
            Searches.AddOrUpdate(args.SearchText, new Search(args), (searchText, search) => new Search(args));
        }
    }

    public class Search
    {
        internal Search(SearchEventArgs e)
        {
            SearchText = e.SearchText;
            Token = e.Token;
            State = e.State;
            Responses = e.Responses.ToList();
        }

        public string SearchText { get; set; }
        public int Token { get; set; }
        public SearchStates State { get; set; }
        public List<SearchResponse> Responses { get; set; }
    }
}
