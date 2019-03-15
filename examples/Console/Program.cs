namespace Console
{
    using Newtonsoft.Json;
    using Soulseek;
    using Soulseek.NET;
    using Soulseek.NET.Messaging.Messages;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Utility.CommandLine;

    public class Program
    {
        [Argument('u', "username")]
        private static string Username { get; set; } = "foo";

        [Argument('p', "password")]
        private static string Password { get; set; }

        [Operands]
        private static string[] Operands { get; set; }

        private static string StdIn { get; set; }

        static async Task SearchAsync(SoulseekClient client, string searchText)
        {
            var result = await client.SearchAsync(string.Join(' ', Operands.Skip(2)));
            Console.WriteLine(JsonConvert.SerializeObject(result));
        }

        static string ReadStdIn()
        {
            if (Console.IsInputRedirected)
            {
                using (StreamReader reader = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
                {
                    return reader.ReadToEnd();
                }
            }

            return null;
        }

        static async Task Main(string[] args)
        {
            StdIn = ReadStdIn();

            Arguments.Populate();

            using (var client = new SoulseekClient(new SoulseekClientOptions(minimumDiagnosticLevel: DiagnosticLevel.Debug)))
            {
                client.StateChanged += Client_ServerStateChanged;
                client.SearchResponseReceived += Client_SearchResponseReceived;
                client.SearchStateChanged += Client_SearchStateChanged;
                client.DownloadProgressUpdated += Client_DownloadProgress;
                client.DownloadStateChanged += Client_DownloadStateChanged;
                client.DiagnosticGenerated += Client_DiagnosticMessageGenerated;
                client.PrivateMessageReceived += Client_PrivateMessageReceived;

                await client.ConnectAsync();
                await client.LoginAsync(Username, Password);

                switch (Operands[1])
                {
                    case "search":
                        await SearchAsync(client, string.Join(' ', Operands.Skip(2)));
                        break;
                    default:
                        Console.WriteLine($"Unknown option: '{Operands[1]}'.");
                        break;                        
                }

                //while (true)
                //{
                //    var cmd = Console.ReadLine();

                //    if (cmd == "disconnect")
                //    {
                //        client.Disconnect();
                //        return;
                //    }
                //    else if (cmd.StartsWith("msg"))
                //    {
                //        var arr = cmd.Split(' ');

                //        var peer = arr.Skip(1).Take(1).FirstOrDefault();
                //        var message = arr.Skip(2).Take(999);

                //        await client.SendPrivateMessageAsync(peer, string.Join(' ', message));
                //    }
                //    else if (cmd.StartsWith("browse"))
                //    {
                //        var peer = cmd.Split(' ').Skip(1).FirstOrDefault();
                //        var result = await client.BrowseAsync(peer);

                //        Console.WriteLine(JsonConvert.SerializeObject(result));
                //        continue;
                //    }
                //    else if (cmd.StartsWith("search"))
                //    {
                //        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(300)))
                //        {
                //            var search = string.Join(' ', cmd.Split(' ').Skip(1));
                //            var token = new Random().Next();
                //            var result = await client.SearchAsync(search, token, new SearchOptions(
                //                filterFiles: false,
                //                filterResponses: false,
                //                fileLimit: 10000), cts.Token);

                //            Console.WriteLine(JsonConvert.SerializeObject(result));
                //            continue;
                //        }
                //    }
                //    else if (cmd.StartsWith("download-folder"))
                //    {
                //        var peer = cmd.Split(' ').Skip(1).FirstOrDefault();

                //        var files = new[]
                //        {
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\01 - Bulls On Parade.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\02 - Down Rodeo.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\03 - People Of The Sun.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\04 - Revolver.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\05 - Roll Right.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\06 - Snakecharmer.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\07 - Tire Me.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\08 - Vietnow.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\09 - Wind Below.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\10 - Without A Face.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\11 - Year Of The Boomerang.mp3",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\Thumbs.db",
                //            @"@@djpnk\\Bootlegs\\Fear Is Your Only God\\album.nfo",
                //        };

                //        var task = Task.Run(() =>
                //        {
                //            var random = new Random();

                //            Parallel.ForEach(files, async (file) =>
                //            {
                //                Console.WriteLine($"Attempting to download {file}");
                //                var bytes = await client.DownloadAsync(peer, file, random.Next());
                //                var filename = $@"C:\tmp\{Path.GetFileName(file)}";

                //                Console.WriteLine($"Bytes received: {bytes.Length}; writing to file {filename}...");
                //                System.IO.File.WriteAllBytes(filename, bytes);
                //                Console.WriteLine("Download complete!");
                //            });
                //        });

                //        await task;

                //        Console.WriteLine($"All files complete.");
                //    }
                //    else if (cmd.StartsWith("download"))
                //    {
                //        var peer = cmd.Split(' ').Skip(1).FirstOrDefault();
                //        var file = string.Join(' ', cmd.Split(' ').Skip(2));

                //        var bytes = await client.DownloadAsync(peer, file, new Random().Next());
                //        var filename = $@"C:\tmp\{Path.GetFileName(file)}";

                //        Console.WriteLine($"Bytes received: {bytes.Length}; writing to file {filename}...");
                //        System.IO.File.WriteAllBytes(filename, bytes);
                //        Console.WriteLine("Download complete!");
                //    }
                //    else
                //    {
                //        try
                //        {
                //            await client.LoginAsync(cmd.Split(' ')[0], cmd.Split(' ')[1]);
                //            Console.WriteLine($"Logged in.");
                //        }
                //        catch (Exception ex)
                //        {
                //            Console.WriteLine($"Login failed: {ex.Message}");
                //        }
                //    }
                //}
            }
        }

        private static void Client_PrivateMessageReceived(object sender, PrivateMessage e)
        {
            Console.WriteLine($"[{e.Timestamp}] [{e.Username}]: {e.Message}");
        }

        private static void Client_DiagnosticMessageGenerated(object sender, DiagnosticGeneratedEventArgs e)
        {
            Console.WriteLine($"[DIAGNOSTICS] [{e.Level}]: {e.Message}");
        }

        private static void Client_DownloadStateChanged(object sender, DownloadStateChangedEventArgs e)
        {
            Console.WriteLine($"[DOWNLOAD] [{e.Filename}]: {e.PreviousState} ==> {e.State}");
        }

        private static void Client_SearchStateChanged(object sender, SearchStateChangedEventArgs e)
        {
            Console.WriteLine($"[SEARCH] [{e.SearchText}]: {e.State}");
        }

        private static ConcurrentDictionary<string, double> Progress { get; set; } = new ConcurrentDictionary<string, double>();

        private static void Client_DownloadProgress(object sender, DownloadProgressUpdatedEventArgs e)
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
