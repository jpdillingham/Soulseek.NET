namespace Console
{
    using Newtonsoft.Json;
    using Soulseek.NET;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            using (var client = new SoulseekClient())
            {
                client.StateChanged += Client_ServerStateChanged;
                client.SearchResponseReceived += Client_SearchResponseReceived;
                client.SearchStateChanged += Client_SearchStateChanged;
                client.DownloadProgress += Client_DownloadProgress;
                client.DownloadStateChanged += Client_DownloadStateChanged;

                await client.ConnectAsync();

                Console.WriteLine("Enter username and password:");

                while (true)
                {
                    var cmd = Console.ReadLine();

                    if (cmd == "disconnect")
                    {
                        client.Disconnect();
                        return;
                    }
                    else if (cmd.StartsWith("browse"))
                    {
                        var peer = cmd.Split(' ').Skip(1).FirstOrDefault();
                        var result = await client.BrowseAsync(peer);

                        Console.WriteLine(JsonConvert.SerializeObject(result));
                        continue;
                    }
                    else if (cmd.StartsWith("start-search"))
                    {
                        var search = string.Join(' ', cmd.Split(' ').Skip(1));
                        var token = new Random().Next();

                        await client.BeginSearchAsync(search, token, new SearchOptions()
                        {
                            FilterFiles = false,
                            FilterResponses = false,
                            FileLimit = 100000,
                        });

                        Console.WriteLine($"Search for {search} started.");
                    }
                    else if (cmd.StartsWith("search"))
                    {
                        var search = string.Join(' ', cmd.Split(' ').Skip(1));
                        var token = new Random().Next();
                        var result = await client.SearchAsync(search, token, new SearchOptions()
                        {
                            FilterFiles = false,
                            FilterResponses = false,
                            FileLimit = 100000,
                        });

                        Console.WriteLine(JsonConvert.SerializeObject(result));
                        continue;
                    }
                    else if (cmd.StartsWith("download-folder"))
                    {
                        var peer = cmd.Split(' ').Skip(1).FirstOrDefault();

                        var files = new[]
                        {
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\01 - Bulls On Parade.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\02 - Down Rodeo.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\03 - People Of The Sun.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\04 - Revolver.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\05 - Roll Right.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\06 - Snakecharmer.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\07 - Tire Me.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\08 - Vietnow.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\09 - Wind Below.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\10 - Without A Face.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\11 - Year Of The Boomerang.mp3",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\Thumbs.db",
                            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\album.nfo",
                        };

                        var task = Task.Run(() =>
                        {
                            var random = new Random();

                            Parallel.ForEach(files, async (file) =>
                            {
                                Console.WriteLine($"Attempting to download {file}");
                                var bytes = await client.DownloadAsync(peer, file, random.Next());
                                var filename = $@"C:\tmp\{Path.GetFileName(file)}";

                                Console.WriteLine($"Bytes received: {bytes.Length}; writing to file {filename}...");
                                System.IO.File.WriteAllBytes(filename, bytes);
                                Console.WriteLine("Download complete!");
                            });
                        });

                        await task;
                        
                        Console.WriteLine($"All files complete.");
                    }
                    else if (cmd.StartsWith("download"))
                    {
                        var peer = cmd.Split(' ').Skip(1).FirstOrDefault();
                        var file = string.Join(' ', cmd.Split(' ').Skip(2));

                        var bytes = await client.DownloadAsync(peer, file, new Random().Next());
                        var filename = $@"C:\tmp\{Path.GetFileName(file)}";

                        Console.WriteLine($"Bytes received: {bytes.Length}; writing to file {filename}...");
                        System.IO.File.WriteAllBytes(filename, bytes);
                        Console.WriteLine("Download complete!");
                    }
                    else
                    {
                        try
                        {
                            await client.LoginAsync(cmd.Split(' ')[0], cmd.Split(' ')[1]);
                            Console.WriteLine($"Logged in.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Login failed: {ex.Message}");
                        }
                    }
                }
            }
        }

        private static void Client_DownloadStateChanged(object sender, DownloadStateChangedEventArgs e)
        {
            Console.WriteLine($"[DOWNLOAD] [{e.Filename}]: {e.State}");
        }

        private static void Client_SearchStateChanged(object sender, SearchStateChangedEventArgs e)
        {
            Console.WriteLine($"[SEARCH] [{e.SearchText}]: {e.State}");
        }

        private static ConcurrentDictionary<string, double> Progress { get; set; } = new ConcurrentDictionary<string, double>();

        private static void Client_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            var key = $"{e.Username}:{e.Filename}:{e.Token}";
            Progress.AddOrUpdate(key, e.PercentComplete, (k, v) =>
            {
                if (Progress[k] <= e.PercentComplete)
                {
                    return e.PercentComplete;
                }

                return Progress[k];
            });

            //Console.WriteLine($"[PROGRESS]: {e.Filename}: {Progress[key]}%");
        }

        private static void Client_SearchResponseReceived(object sender, SearchResponseReceivedEventArgs e)
        {
            Console.WriteLine($"[SEARCH RESPONSE] [{e.SearchText}]: {e.Response.FileCount} files from {e.Response.Username}");
            //var r = e.Response;

            //Console.WriteLine($"=====================================================================================");
            //Console.WriteLine($"New search result from: {r.Username} (slots: {r.FreeUploadSlots}, upload: {r.UploadSpeed}, queue: {r.QueueLength})");

            //foreach (var file in r.Files)
            //{
            //    Console.WriteLine($"[{file.BitRate}/{file.SampleRate}/{file.BitDepth}] {file.Filename}");
            //}
        }

        private static void Client_ServerStateChanged(object sender, SoulseekClientStateChangedEventArgs e)
        {
            Console.WriteLine($"Server state changed to {e.State} ({e.Message})");
        }
    }
}
