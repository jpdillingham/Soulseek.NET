namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;

    /// <summary>
    ///     Tracks active searches.
    /// </summary>
    public class SearchTracker : ISearchTracker
    {
        /// <summary>
        ///     Gets active searches.
        /// </summary>
        public ConcurrentDictionary<string, Search> Searches { get; private set; } =
            new ConcurrentDictionary<string, Search>();

        /// <summary>
        ///     Adds or updates a tracked search.
        /// </summary>
        /// <param name="args"></param>
        public void AddOrUpdate(SearchResponseReceivedEventArgs args)
        {
            Searches.AddOrUpdate(args.Search.SearchText, args.Search, (searchText, search) => args.Search);
        }

        /// <summary>
        ///     Adds or updates a tracked search.
        /// </summary>
        /// <param name="args"></param>
        public void AddOrUpdate(SearchStateChangedEventArgs args)
        {
            Searches.AddOrUpdate(args.Search.SearchText, args.Search, (searchText, search) => args.Search);
        }

        /// <summary>
        ///     Removes all tracked searches.
        /// </summary>
        public void Clear()
        {
            Searches.Clear();
        }

        /// <summary>
        ///     Removes a tracked search.
        /// </summary>
        /// <param name="searchText"></param>
        public void TryRemove(string searchText)
        {
            Searches.TryRemove(searchText, out _);
        }
    }
}