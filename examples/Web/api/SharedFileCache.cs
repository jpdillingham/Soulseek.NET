namespace WebAPI
{
    using Microsoft.Data.Sqlite;
    using Soulseek;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    public interface ISharedFileCache
    {
        IEnumerable<Soulseek.File> Search(SearchQuery query);
    }

    public class SharedFileCache : ISharedFileCache
    {
        public SharedFileCache(string directory, long ttl)
        {
            Directory = directory;
            TTL = ttl;
        }

        private SqliteConnection SQLite { get; set; }
        private Dictionary<string, Soulseek.File> Files { get; set; }
        private long TTL { get; }
        private string Directory { get; }
        private DateTime? LastFill { get; set; }

        private void CreateTable()
        {
            SQLite = new SqliteConnection("Data Source=:memory:");
            SQLite.Open();

            using (var cmd = new SqliteCommand("CREATE VIRTUAL TABLE files USING fts5(filename)", SQLite))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private IEnumerable<Soulseek.File> QueryTable(string text)
        {
            var query = $"SELECT * FROM files WHERE files MATCH '\"{text.Replace("'", "''")}\"'";

            try
            {
                using (var cmd = new SqliteCommand(query, SQLite))
                {
                    var results = new List<string>();
                    var reader = cmd.ExecuteReader();

                    while (reader.Read())
                    {
                        results.Add(reader.GetString(0));
                    }

                    return results.Select(r => Files[r.Replace("''", "'")]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(query);
                return Enumerable.Empty<Soulseek.File>();
            }
        }

        private void InsertFile(string filename)
        {
            using (var cmd = new SqliteCommand($"INSERT INTO files(filename) VALUES('{filename.Replace("'", "''")}')", SQLite))
            {
                cmd.ExecuteNonQuery();
            }
        }

        private void FillTable()
        {
            var sw = new Stopwatch();
            sw.Start();

            Console.WriteLine($"[SHARED FILE CACHE]: Refreshing...");

            CreateTable();

            Files = System.IO.Directory.GetFiles(Directory, "*", SearchOption.AllDirectories)
                .Select(f => new Soulseek.File(1, f, new FileInfo(f).Length, Path.GetExtension(f), 0))
                .ToDictionary(f => f.Filename, f => f);

            foreach (var file in Files)
            {
                InsertFile(file.Key);
            }

            sw.Stop();

            Console.WriteLine($"[SHARED FILE CACHE]: Refreshed in {sw.ElapsedMilliseconds}ms.  Found {Files.Count()} files.");
            LastFill = DateTime.UtcNow;
        }

        public IEnumerable<Soulseek.File> Search(SearchQuery query)
        {
            if (!LastFill.HasValue || LastFill.Value.AddMilliseconds(TTL) < DateTime.UtcNow)
            {
                FillTable();
            }

            // sanitize the query string.  there's probably more to it than this.
            var queryText = query.Query
                .Replace("/", " ")
                .Replace("\\", " ")
                .Replace(":", " ");

            return QueryTable(queryText);
        }
    }
}