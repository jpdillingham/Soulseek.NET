namespace WebAPI
{
    using Soulseek;
    using System;
    using System.Collections.Concurrent;
    using System.Net;

    public interface IDownloadTracker
    {
        ConcurrentDictionary<string, ConcurrentDictionary<string, Download>> Downloads { get; }
        void AddOrUpdate(DownloadEventArgs state);
    }

    public class DownloadTracker : IDownloadTracker
    {
        public ConcurrentDictionary<string, ConcurrentDictionary<string, Download>> Downloads { get; private set; } = 
            new ConcurrentDictionary<string, ConcurrentDictionary<string, Download>>();

        public void AddOrUpdate(DownloadEventArgs args)
        {
            Downloads.AddOrUpdate(args.Username, GetNewDictionary(args), (user, dict) =>
            {
                dict.AddOrUpdate(args.Filename, new Download(args), (file, download) => new Download(args));
                return dict;
            });
        }

        private ConcurrentDictionary<string, Download> GetNewDictionary(DownloadEventArgs args)
        {
            var r = new ConcurrentDictionary<string, Download>();
            r.AddOrUpdate(args.Filename, new Download(args), (key, value) => new Download(args));
            return r;
        }
    }

    public class Download
    {
        internal Download(DownloadEventArgs e)
        {
            Filename = e.Filename;
            Username = e.Username;
            AverageSpeed = e.AverageSpeed;
            BytesDownloaded = e.BytesDownloaded;
            BytesRemaining = e.BytesRemaining;
            ElapsedTime = e.ElapsedTime;
            EndTime = e.EndTime;
            IPAddress = e.IPAddress;
            PercentComplete = e.PercentComplete;
            Port = e.Port;
            RemainingTime = e.RemainingTime;
            RemoteToken = e.RemoteToken;
            Size = e.Size;
            StartTime = e.StartTime;
            State = e.State;
            Token = e.Token;
        }

        public string Filename { get; }
        public string Username { get; }
        public double AverageSpeed { get; }
        public int BytesDownloaded { get; }
        public int BytesRemaining { get; }
        public TimeSpan? ElapsedTime { get; }
        public DateTime? EndTime { get; }
        public IPAddress IPAddress { get; }
        public double PercentComplete { get; }
        public int? Port { get; }
        public TimeSpan? RemainingTime { get; }
        public int RemoteToken { get; }
        public int Size { get; }
        public DateTime? StartTime { get; }
        public DownloadStates State { get; }
        public int Token { get; }
    }
}
