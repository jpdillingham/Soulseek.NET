namespace WebAPI
{
    using Soulseek;
    using System;
    using System.Collections.Generic;
    using System.Net;

    public interface IDownloadTracker
    {
        Dictionary<string, Dictionary<string, Download>> Downloads { get; }
        void AddOrUpdate(DownloadEventArgs state);
    }

    public class DownloadTracker : IDownloadTracker
    {
        public Dictionary<string, Dictionary<string, Download>> Downloads { get; private set; } = 
            new Dictionary<string, Dictionary<string, Download>>();

        public void AddOrUpdate(DownloadEventArgs args)
        {
            if (Downloads.ContainsKey(args.Username))
            {
                if (Downloads[args.Username].ContainsKey(args.Filename))
                {
                    Downloads[args.Username][args.Filename] = new Download(args);
                }
                else
                {
                    Downloads[args.Username].Add(args.Filename, new Download(args));
                }
            }
            else
            {
                Downloads.Add(args.Username, new Dictionary<string, Download>()
                {
                    { args.Filename, new Download(args) }
                });
            }
        }
    }

    public class Download
    {
        internal Download(DownloadEventArgs e)
        {
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
