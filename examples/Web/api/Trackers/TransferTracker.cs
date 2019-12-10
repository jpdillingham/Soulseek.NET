namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.IO;
    using System.Linq;

    public static class TransferTrackerExtensions
    {
        public static ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> WithDirection(
            this ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> allTransfers,
            TransferDirection direction)
        {
            allTransfers.TryGetValue(direction, out var transfers);
            return transfers ?? new ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>();
        }

        public static object ToMap(
            this ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> directedTransfers)
        {
            return directedTransfers.Select(u => new
            {
                Username = u.Key,
                Directories = u.Value.Values
                     .GroupBy(f => Path.GetDirectoryName(f.Transfer.Filename))
                     .Select(d => new { Directory = d.Key, Files = d.Select(r => r.Transfer)})
            });
        }

        public static ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)> FromUser(
            this ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>> directedTransfers,
            string username)
        {
            directedTransfers.TryGetValue(username, out var transfers);
            return transfers ?? new ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>();
        }

        public static object ToMap(
            this ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)> userTransfers)
        {
            return userTransfers.Values
                .GroupBy(f => Path.GetDirectoryName(f.Transfer.Filename))
                .Select(d => new { Directory = d.Key, Files = d.Select(r => r.Transfer)});
        }

        public static (Transfer Transfer, CancellationTokenSource CancellationTokenSource) WithFilename(
            this ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)> userTransfers,
            string filename)
        {
            userTransfers.TryGetValue(filename, out var transfer);
            return transfer;
        }
    }

    /// <summary>
    ///     Tracks transfers.
    /// </summary>
    public class TransferTracker : ITransferTracker
    {
        /// <summary>
        ///     Tracked transfers.
        /// </summary>
        public ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>> Transfers { get; private set; } =
            new ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer, CancellationTokenSource)>>>();

        public TransferTracker()
        {
            Transfers.TryAdd(TransferDirection.Download, new ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>());
            Transfers.TryAdd(TransferDirection.Upload, new ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>>());
        }

        /// <summary>
        ///     Adds or updates a tracked transfer.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="cancellationTokenSource"></param>
        public void AddOrUpdate(TransferEventArgs args, CancellationTokenSource cancellationTokenSource)
        {
            Transfers.TryGetValue(args.Transfer.Direction, out var direction);

            direction.AddOrUpdate(args.Transfer.Username, GetNewDictionaryForUser(args, cancellationTokenSource), (user, dict) =>
            {
                dict.AddOrUpdate(args.Transfer.Filename, (args.Transfer, cancellationTokenSource), (file, record) => (args.Transfer, cancellationTokenSource));
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

        /// <summary>
        ///     Gets the specified transfer.
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="username"></param>
        /// <param name="filename"></param>
        /// <param name="transfer"></param>
        /// <returns></returns>
        public bool TryGet(TransferDirection direction, string username, string filename, out (Transfer Transfer, CancellationTokenSource CancellationTokenSource) transfer)
        {
            transfer = default;

            if (Transfers.TryGetValue(direction, out var transfers))
            {
                if (transfers.TryGetValue(username, out var user))
                {
                    if (user.TryGetValue(filename, out transfer))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)> GetNewDictionaryForUser(TransferEventArgs args, CancellationTokenSource cancellationTokenSource)
        {
            var r = new ConcurrentDictionary<string, (Transfer Transfer, CancellationTokenSource CancellationTokenSource)>();
            r.AddOrUpdate(args.Transfer.Filename, (args.Transfer, cancellationTokenSource), (file, record) => (args.Transfer, record.CancellationTokenSource));
            return r;
        }
    }
}