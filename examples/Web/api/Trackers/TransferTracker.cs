namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;
    using System.Threading;

    /// <summary>
    ///     Tracks transfers.
    /// </summary>
    public class TransferTracker : ITransferTracker
    {
        /// <summary>
        ///     Tracked transfers.
        /// </summary>
        public ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationToken CancellationToken)>>> Transfers { get; private set; } =
            new ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer, CancellationToken)>>>();

        /// <summary>
        ///     Adds or updates a tracked transfer.
        /// </summary>
        /// <param name="args"></param>
        public void AddOrUpdate(TransferEventArgs args)
        {
            Transfers.TryGetValue(args.Transfer.Direction, out var direction);

            direction.AddOrUpdate(args.Transfer.Username, GetNewDictionaryForUser(args), (user, dict) =>
            {
                dict.AddOrUpdate(args.Transfer.Filename, (args.Transfer, new CancellationToken()), (file, record) => (args.Transfer, record.CancellationToken));
                return dict;
            });
        }

        /// <summary>
        ///     Removes a tracked transfer.
        /// </summary>
        /// <remarks>Omitting a filename will remove ALL transfers associated with the specified username.</remarks>
        public void TryRemove(TransferDirection direction, string username, string filename = null)
        {
            Transfers.TryGetValue(direction, out var directionDict);

            if (string.IsNullOrEmpty(filename))
            {
                directionDict.TryRemove(username, out var _);
            }
            else
            {
                directionDict.TryGetValue(username, out var userDict);
                userDict.TryRemove(filename, out var _);
            }
        }

        private ConcurrentDictionary<string, (Transfer Transfer, CancellationToken CancellationToken)> GetNewDictionaryForUser(TransferEventArgs args)
        {
            var r = new ConcurrentDictionary<string, (Transfer Transfer, CancellationToken CancellationToken)>();
            r.AddOrUpdate(args.Transfer.Filename, (args.Transfer, new CancellationToken()), (file, record) => (args.Transfer, record.CancellationToken));
            return r;
        }
    }
}