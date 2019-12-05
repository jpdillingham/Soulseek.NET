namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;
    using System.Threading;

    public class TransferTracker : ITransferTracker
    {
        public ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationToken CancellationToken)>>> Transfers { get; private set; } = 
            new ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer, CancellationToken)>>>();

        public void AddOrUpdate(TransferEventArgs args)
        {
            Transfers.TryGetValue(args.Transfer.Direction, out var direction);

            direction.AddOrUpdate(args.Transfer.Username, GetNewDictionaryForUser(args), (user, dict) =>
            {
                dict.AddOrUpdate(args.Transfer.Filename, (args.Transfer, new CancellationToken()), (file, record) => (args.Transfer, record.CancellationToken));
                return dict;
            });
        }

        private ConcurrentDictionary<string, (Transfer Transfer, CancellationToken CancellationToken)> GetNewDictionaryForUser(TransferEventArgs args)
        {
            var r = new ConcurrentDictionary<string, (Transfer Transfer, CancellationToken CancellationToken)>();
            r.AddOrUpdate(args.Transfer.Filename, (args.Transfer, new CancellationToken()), (file, record) => (args.Transfer, record.CancellationToken));
            return r;
        }
    }
}
