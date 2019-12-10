namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;
    using System.Threading;

    /// <summary>
    ///     Tracks transfers.
    /// </summary>
    public interface ITransferTracker
    {
        /// <summary>
        ///     Tracked transfers.
        /// </summary>
        ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> Transfers { get; }

        /// <summary>
        ///     Adds or updates a tracked transfer.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="cancellationTokenSource"></param>
        void AddOrUpdate(TransferEventArgs args, CancellationTokenSource cancellationTokenSource);

        /// <summary>
        ///     Removes a tracked transfer.
        /// </summary>
        /// <remarks>Omitting a filename will remove ALL transfers associated with the specified username.</remarks>
        void TryRemove(TransferDirection direction, string username, string filename = null);

        /// <summary>
        ///     Gets the specified transfer.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="username"></param>
        /// <param name="filename"></param>
        /// <param name="transfer"></param>
        /// <returns></returns>
        bool TryGet(TransferDirection direction, string username, string filename, out (Transfer Transfer, CancellationTokenSource CancellationTokenSource) transfer);
    }
}