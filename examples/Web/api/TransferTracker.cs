namespace WebAPI
{
    using Soulseek;
    using System;
    using System.Collections.Concurrent;
    using System.Net;

    public interface ITransferTracker
    {
        ConcurrentDictionary<string, ConcurrentDictionary<string, Transfer>> Downloads { get; }
        ConcurrentDictionary<string, ConcurrentDictionary<string, Transfer>> Uploads { get; }
        void AddOrUpdate(TransferEventArgs args);
    }

    public class TransferTracker : ITransferTracker
    {
        public ConcurrentDictionary<string, ConcurrentDictionary<string, Transfer>> Downloads { get; private set; } = 
            new ConcurrentDictionary<string, ConcurrentDictionary<string, Transfer>>();

        public ConcurrentDictionary<string, ConcurrentDictionary<string, Transfer>> Uploads { get; private set; } =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, Transfer>>();

        public void AddOrUpdate(TransferEventArgs args)
        {
            var direction = args.Transfer.Direction == TransferDirection.Download ? Downloads : Uploads;

            direction.AddOrUpdate(args.Transfer.Username, GetNewDictionary(args), (user, dict) =>
            {
                dict.AddOrUpdate(args.Transfer.Filename, args.Transfer, (file, transfer) => args.Transfer);
                return dict;
            });
        }

        private ConcurrentDictionary<string, Transfer> GetNewDictionary(TransferEventArgs args)
        {
            var r = new ConcurrentDictionary<string, Transfer>();
            r.AddOrUpdate(args.Transfer.Filename, args.Transfer, (key, value) => args.Transfer);
            return r;
        }
    }
}
