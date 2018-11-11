namespace Console
{
    using Newtonsoft.Json;
    using Soulseek.NET;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            using (var client = new SoulseekClient())
            {
                client.ConnectionStateChanged += Client_ServerStateChanged;
                client.SearchResponseReceived += Client_SearchResponseReceived;

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
                    else if (cmd.StartsWith("search"))
                    {
                        var search = string.Join(' ', cmd.Split(' ').Skip(1));
                        var result = await client.SearchAsync(search, new SearchOptions()
                        {
                            FilterFiles = false,
                            FilterResponses = false,
                            FileLimit = 100000,
                        });

                        Console.WriteLine(JsonConvert.SerializeObject(result));
                        continue;
                    }
                    else if (cmd.StartsWith("download"))
                    {
                        var result = await client.DownloadAsync("praetor-", @"@@djpnk\Bootlegs\Staind - Mudshuvel.mp3");

                        Console.WriteLine(JsonConvert.SerializeObject(result));
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

        private static void Client_SearchResponseReceived(object sender, SearchResponseReceivedEventArgs e)
        {
            var r = e.Response;

            Console.WriteLine($"=====================================================================================");
            Console.WriteLine($"New search result from: {r.Username} (slots: {r.FreeUploadSlots}, upload: {r.UploadSpeed}, queue: {r.QueueLength})");

            foreach (var file in r.Files)
            {
                Console.WriteLine($"[{file.BitRate}/{file.SampleRate}/{file.BitDepth}] {file.Filename}");
            }
        }

        private static void Client_ServerStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"Server state changed to {e.State} ({e.Message})");
        }
    }
}
