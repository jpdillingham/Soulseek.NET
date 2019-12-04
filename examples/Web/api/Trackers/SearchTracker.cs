namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;

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

        public void TryRemove(string searchText)
        {
            Searches.TryRemove(searchText, out _);
        }
    }
}
