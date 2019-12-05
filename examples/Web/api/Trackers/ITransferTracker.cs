namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;
    using System.Threading;

    public interface ITransferTracker
    {
        ConcurrentDictionary<TransferDirection, ConcurrentDictionary<string, ConcurrentDictionary<string, (Transfer Transfer, CancellationToken CancellationToken)>>> Transfers { get; }
        void AddOrUpdate(TransferEventArgs args);
    }
}
