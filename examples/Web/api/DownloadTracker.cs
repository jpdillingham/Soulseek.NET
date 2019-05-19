namespace WebAPI
{
    using Soulseek;
    using System.Collections.Generic;

    public interface IDownloadTracker
    {
        Dictionary<string, Dictionary<string, DownloadEventArgs>> Downloads { get; }
        void AddOrUpdate(DownloadEventArgs state);
    }

    public class DownloadTracker : IDownloadTracker
    {
        public Dictionary<string, Dictionary<string, DownloadEventArgs>> Downloads { get; private set; } = 
            new Dictionary<string, Dictionary<string, DownloadEventArgs>>();

        public void AddOrUpdate(DownloadEventArgs args)
        {
            if (Downloads.ContainsKey(args.Username))
            {
                if (Downloads[args.Username].ContainsKey(args.Filename))
                {
                    Downloads[args.Username][args.Filename] = args;
                }
                else
                {
                    Downloads[args.Username].Add(args.Filename, args);
                }
            }
            else
            {
                Downloads.Add(args.Username, new Dictionary<string, DownloadEventArgs>()
                {
                    { args.Filename, args }
                });
            }
        }
    }
}
