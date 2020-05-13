namespace WebAPI
{
    using Soulseek;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public interface ISharedFileCache
    {
        string Directory { get; }
        IEnumerable<Soulseek.File> Files { get; }
        long TTL { get; }
        IEnumerable<Soulseek.File> Search(SearchQuery query);
    }

    public class SharedFileCache : ISharedFileCache
    {
        public SharedFileCache(string directory, long ttl)
        {
            Directory = directory;
            TTL = ttl;
        }

        public IEnumerable<Soulseek.File> Files { get; private set; }
        public long TTL { get; }
        public string Directory { get; }
        private DateTime? LastFill { get; set; }

        private void Fill()
        {
            var sw = new Stopwatch();
            sw.Start();

            Console.WriteLine($"[SHARED FILE CACHE]: Refreshing...");

            Files = System.IO.Directory.GetFiles(Directory, "*", SearchOption.AllDirectories)
                .Select(f => new Soulseek.File(1, f, new FileInfo(f).Length, Path.GetExtension(f), 0));

            sw.Stop();

            Console.WriteLine($"[SHARED FILE CACHE]: Refreshed in {sw.ElapsedMilliseconds}ms.  Found {Files.Count()} files.");
            LastFill = DateTime.UtcNow;
        }

        public IEnumerable<Soulseek.File> Search(SearchQuery query)
        {
            if (!LastFill.HasValue || LastFill.Value.AddMilliseconds(TTL) < DateTime.UtcNow)
            {
                Fill();
            }

            // sanitize the query string.  there's probably more to it than this.
            var queryText = query.Query
                .Replace("/", " ")
                .Replace("\\", " ")
                .Replace(":", " ");

            var words = queryText.Split(' ');

            return Files.Where(file =>
                words.All(word => file.Filename.Contains(word, StringComparison.InvariantCultureIgnoreCase)));
        }
    }
}