namespace WebAPI.Trackers
{
    using System.Collections.Concurrent;

    /// <summary>
    ///     Tracks browse operations.
    /// </summary>
    interface IBrowseTracker
    {
        /// <summary>
        ///     Tracked browse operations.
        /// </summary>
        ConcurrentDictionary<string, decimal> Browses { get; }

        /// <summary>
        ///     Adds or updates a tracked browse operation.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="progress"></param>
        void AddOrUpdate(string username, decimal progress);

        /// <summary>
        ///     Removes a tracked browse operation for the specified user.
        /// </summary>
        /// <param name="username"></param>
        void TryRemove(string username);

        /// <summary>
        ///     Gets the browse progress for the specified user.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        bool TryGet(string username, out decimal progress);
    }
}
