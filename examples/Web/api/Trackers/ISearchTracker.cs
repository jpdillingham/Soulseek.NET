namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;

    /// <summary>
    ///     Tracks active searches.
    /// </summary>
    public interface ISearchTracker
    {
        /// <summary>
        ///     Gets active searches.
        /// </summary>
        ConcurrentDictionary<string, Search> Searches { get; }

        /// <summary>
        ///     Adds or updates a tracked search.
        /// </summary>
        /// <param name="args"></param>
        void AddOrUpdate(SearchResponseReceivedEventArgs args);

        /// <summary>
        ///     Adds or updates a tracked search.
        /// </summary>
        /// <param name="args"></param>
        void AddOrUpdate(SearchStateChangedEventArgs args);

        /// <summary>
        ///     Removes all tracked searches.
        /// </summary>
        void Clear();

        /// <summary>
        ///     Removes a tracked search.
        /// </summary>
        /// <param name="searchText"></param>
        void TryRemove(string searchText);
    }
}