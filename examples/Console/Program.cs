namespace Console
{
    using Console.Model;
    using global::Utility.CommandLine;
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

    public class Program
    {
        [Argument('u', "username")]
        private static string Username { get; set; } = "foo";

        [Argument('p', "password")]
        private static string Password { get; set; }

        [Argument('a', "artist")]
        private static string Artist { get; set; }

        [Argument('l', "album")]
        private static string Album { get; set; } = string.Empty;

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

        static async Task<Artist> SelectArtist(string artist)
        {
            o($"\nSearching for artist '{artist}'...");

            var artists = await MusicBrainz.GetMatchingArtists(artist);
            var artistList = artists.OrderByDescending(a => a.Score).ToList();

            var longest = artistList.Max(a => a.DisambiguatedName.Length);

            o($"\nMatching artists:\n");

            for (int i = 0; i < artistList.Count; i++)
            {
                o($"  {(i + 1).ToString().PadLeft(3)}.  {artistList[i].DisambiguatedName.PadRight(longest)}  {artistList[i].Score.ToString().PadLeft(3)}%");
            }

            Console.WriteLine();

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
                    Console.Write($"Invalid input.  ");
                }
            } while (true);
        }

        static async Task<ReleaseGroup> SelectReleaseGroup(Artist artist, string album)
        {
            o($"\nSearching for '{artist.Name}' release groups matching '{album}'...");

            var releaseGroups = await MusicBrainz.GetArtistReleaseGroups(Guid.Parse(artist.ID));
            var releaseGroupList = releaseGroups
                .OrderBy(r => r.Type)
                .ThenBy(r => r.Year, new SemiNumericComparer())
                .ThenBy(r => r.DisambiguatedTitle)
                .ToList();

            releaseGroupList = releaseGroupList.Select(r => {
                r.Score = r.Title.SimilarityCaseInsensitive(album);
                return r;
            }).ToList();

            var longest = releaseGroupList.Max(r => r.DisambiguatedTitle.Length);
            var bestMatch = releaseGroupList.OrderByDescending(r => r.Score).First().ID;
            var bestIndex = 0;

            var lastType = string.Empty;

            o($"\nMatching release groups:");

            for (int i = 0; i < releaseGroupList.Count; i++)
            {
                var r = releaseGroupList[i];

                if (r.ID == bestMatch)
                {
                    bestIndex = i + 1;
                }

                if (lastType != r.Type)
                {
                    Console.WriteLine($"\n{r.Type}\n");
                    lastType = r.Type;
                }

                o($"  {(i + 1).ToString().PadLeft(3)}.  {r.Year}  {r.DisambiguatedTitle.PadRight(longest)}  {Math.Round(r.Score * 100, 0).ToString().PadLeft(3)}% {(r.ID == bestMatch ? "<===" : string.Empty)}");
            }

            Console.WriteLine();

            do
            {
                Console.Write($"Select release group (1-{releaseGroupList.Count}, best match: {bestIndex}): ");

                var selection = Console.ReadLine();

                try
                {
                    var num = Int32.Parse(selection) - 1;
                    return releaseGroupList[num];
                }
                catch (Exception)
                {
                    Console.Write($"Invalid input.  ");
                }
            } while (true);
        }

        static async Task<Release> SelectRelease(ReleaseGroup releaseGroup)
        {
            o($"\nSearching for releases in release group '{releaseGroup.Title}'...");

            var releases = await MusicBrainz.GetReleaseGroupReleases(Guid.Parse(releaseGroup.ID));
            var releaseList = releases
                .OrderBy(r => r.Date.ToFuzzyDateTime())
                .ToList();

            var longest = releases.Max(r => r.DisambiguatedTitle.Length);
            var longestFormat = releases.Max(r => r.Format.Length);
            var longestTrackCount = releases.Max(r => r.TrackCount.Length);

            o($"\nReleases:\n");

            for (int i = 0; i < releaseList.Count; i++)
            {
                var r = releaseList[i];
                var format = string.Join("+", r.Media.Select(m => m.Format));
                var tracks = string.Join("+", r.Media.Select(m => m.TrackCount));
                o($"  {(i + 1).ToString().PadLeft(3)}.  {r.Date.ToFuzzyDateTime().ToString("yyyy-MM-dd")}  {r.DisambiguatedTitle.PadRight(longest)}  {r.Format.PadRight(longestFormat)}  {r.TrackCount.PadRight(longestTrackCount)}  {r.Country}");
            }

            Console.WriteLine();

            //Console.WriteLine(JsonConvert.SerializeObject(releases));

            do
            {
                Console.Write($"Select release (1-{releaseList.Count}): ");

                var selection = Console.ReadLine();

                try
                {
                    var num = Int32.Parse(selection) - 1;
                    return releaseList[num];
                }
                catch (Exception)
                {
                    Console.Write($"Invalid input.  ");
                }
            } while (true);
        }

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Arguments.Populate(clearExistingValues: false);

            var artist = await SelectArtist(Artist);

            o($"Selected artist: {artist.DisambiguatedName}");

            var releaseGroup = await SelectReleaseGroup(artist, Album);

            o($"Selected release group: {releaseGroup.DisambiguatedTitle}");

            var release = await SelectRelease(releaseGroup);

            o($"Selected release: {release.DisambiguatedTitle}");

            o(JsonConvert.SerializeObject(release));

            Console.ReadKey();

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
