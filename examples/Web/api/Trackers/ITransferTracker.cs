namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;

    public interface ITransferTracker
    {
        ConcurrentDictionary<string, ConcurrentDictionary<string, Transfer>> Downloads { get; }
        ConcurrentDictionary<string, ConcurrentDictionary<string, Transfer>> Uploads { get; }
        void AddOrUpdate(TransferEventArgs args);
    }
}
