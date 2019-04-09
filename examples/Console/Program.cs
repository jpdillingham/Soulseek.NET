namespace Console
{
    using Console.Model;
    using Newtonsoft.Json;
    using Soulseek;
    using Soulseek.NET;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Utility.CommandLine;

    public class Program
    {
        [Argument('u', "username")]
        private static string Username { get; set; } = "foo";

        [Argument('p', "password")]
        private static string Password { get; set; }

        [Argument('a', "artist")]
        private static string Artist { get; set; }

        [Argument('l', "album")]
        private static string Album { get; set; }

        private static readonly Action<string> o = (s) => Console.WriteLine(s);

        static async Task SearchAsync(SoulseekClient client, BrainzArtist brainz, string searchText)
        {

            var album = brainz.Albums[0]; // one album at a time for now.
            var trackCount = album.Tracks.Count();

            o($"Searching for '{brainz.Artist} {album.Title}'...");

            IEnumerable<SearchResponse> responses = await client.SearchAsync(searchText,
                new SearchOptions(
                    filterResponses: true,
                    minimumResponseFileCount: trackCount,
                    filterFiles: true,
                    ignoredFileExtensions: new string[] { "flac", "m4a" }
                ));

            o($"Total results: {responses.Count()}");

            responses = responses.Where(r => r.FileCount >= trackCount);
            o($"Results with track count >= {trackCount}: {responses.Count()}");

            var bannedUsers = new string[] {  };
            responses = responses.Where(r => !bannedUsers.Contains(r.Username));

            responses = responses.Where(r => TracksMatch(album, r));
            o($"Results with matching tracks: {responses.Count()}");

            var freeResponses = responses.Where(r => r.FreeUploadSlots > 0);
            SearchResponse bestResponse = null;

            if (freeResponses.Any())
            {
                responses = freeResponses;
                o($"Users with free upload slots: {responses.Count()}");

                bestResponse = responses
                    .OrderByDescending(r => r.UploadSpeed)
                    .First();
            }
            else
            {
                o($"No users with free upload slots.");

                bestResponse = responses
                    .OrderBy(r => r.QueueLength)
                    .First();
            }

            o($"Best response from: {bestResponse.Username}");

            var maxLen = bestResponse.Files.Max(f => f.Filename.Length);

            foreach (var file in bestResponse.Files)
            {
                o($"{file.Filename.PadRight(maxLen)}\t{file.Length}\t{file.BitRate}\t{file.Size}");
            }

            await DownloadFilesAsync(client, bestResponse.Username, bestResponse.Files.Select(f => f.Filename));

            Console.WriteLine($"All files complete.");
        }

        private static async Task DownloadFilesAsync(SoulseekClient client, string username, IEnumerable<string> files)
        {
            var random = new Random();

            var tasks = files.Select(async file =>
            {
                Console.WriteLine($"Attempting to download {file}");
                try
                {
                    var bytes = await client.DownloadAsync(username, file, random.Next());

                    var path = $@"C:\tmp\" + Path.GetDirectoryName(file).Replace(Path.GetDirectoryName(Path.GetDirectoryName(file)), "");

                    if (!System.IO.Directory.Exists(path))
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }

                    var filename = Path.Combine(path, Path.GetFileName(file));

                    Console.WriteLine($"Bytes received: {bytes.Length}; writing to file {filename}...");
                    System.IO.File.WriteAllBytes(filename, bytes);
                    Console.WriteLine("Download complete!");
                }
                catch (Exception ex)
                {
                    o($"Error downloading {file}: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }

        private static bool TracksMatch(BrainzAlbum album, SearchResponse response)
        {
            return true;
            o($"Checking response...");


            foreach (var track in response.Files)
            {
                //o($"{Path.GetFileNameWithoutExtension(track.Filename)} [{track.Length / 1000}]");
            }

            foreach (var track in album.Tracks)
            {
                var match = response.Files
                    //.Where(f => f.Length >= (track.Length /1000)) // make sure length is at least as long
                    //.Where(f => f.Length < (track.Length / 1000) + 5) // allow for songs up to 5 seconds longer
                    .Select(f => Path.GetFileNameWithoutExtension(f.Filename))
                        .Where(f => f.Contains(track.Title, StringComparison.InvariantCultureIgnoreCase))
                        .Where(f => f.Contains(track.Number, StringComparison.InvariantCultureIgnoreCase))
                        .Any();

                if (!match)
                {
                    return false;
                }
            }

            return true;
        }

        static bool TryGetBrainzInput(out BrainzArtist brainzArtist)
        {
            var stdin = string.Empty;
            brainzArtist = null;

            if (Console.IsInputRedirected)
            {
                using (StreamReader reader = new StreamReader(Console.OpenStandardInput(), Console.InputEncoding))
                {
                    stdin = reader.ReadToEnd();
                }

                try
                {
                    brainzArtist = JsonConvert.DeserializeObject<BrainzArtist>(stdin);
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            return false;
        }

        static async Task<Artist> SelectArtist(string search)
        {
            o($"Searching for artist '{Artist}'...");

            var artists = await MusicBrainz.GetMatchingArtists(Artist);
            var artistList = artists.Artists.OrderByDescending(a => a.Score).ToList();

            var longest = artistList.Max(a => a.DisambiguatedName.Length);

            o($"      {"Artist".PadRight(longest)}  Score");
            o($"━━━━━━{"━".PadRight(longest, '━')}━━━━━━━");

            for (int i = 0; i < artistList.Count; i++)
            {
                o($"{(i + 1).ToString().PadLeft(3)}.  {artistList[i].DisambiguatedName.PadRight(longest)}  {artistList[i].Score.ToString().PadLeft(3)}%");
            }

            do
            {
                Console.Write($"Select artist (1-{artistList.Count}): ");

                var selection = Console.ReadLine();

                try
                {
                    var num = Int32.Parse(selection) - 1;
                    return artistList[num];
                }
                catch (Exception)
                {
                    o($"Invalid input.");
                }
            } while (true);
        }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Arguments.Populate();

            var artist = await SelectArtist(Artist);

            o($"Selected artist: {artist.DisambiguatedName}");


            var options = new SoulseekClientOptions(
                minimumDiagnosticLevel: DiagnosticLevel.Info,
                peerConnectionOptions: new ConnectionOptions(connectTimeout: 30, readTimeout: 30),
                transferConnectionOptions: new ConnectionOptions(connectTimeout: 30, readTimeout: 10)
            );

            using (var client = new SoulseekClient(options))
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

        private static ConcurrentDictionary<string, ProgressBar> Progress { get; set; } = new ConcurrentDictionary<string, ProgressBar>();

        private static void Client_DownloadProgress(object sender, DownloadProgressUpdatedEventArgs e)
        {
            var key = $"{e.Username}:{e.Filename}:{e.Token}";
            Progress.AddOrUpdate(key, new ProgressBar(30, 0, 100, 1, (int)e.PercentComplete), (k, v) =>
            {
                Progress[k].Value = (int)e.PercentComplete;
                return Progress[k];
            });

            Console.Write($"\r[PROGRESS]: {e.Filename}: {Progress[key]}%");

            if (e.PercentComplete == 100)
            {
                Console.Write("\n");
            }
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
