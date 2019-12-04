namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;

    public interface ISearchTracker
    {
        ConcurrentDictionary<string, Search> Searches { get; }
        void AddOrUpdate(SearchResponseReceivedEventArgs args);
        void AddOrUpdate(SearchStateChangedEventArgs args);
        void TryRemove(string searchText);
        void Clear();
    }
}
