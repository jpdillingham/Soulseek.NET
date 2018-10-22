namespace Console
{
    using Newtonsoft.Json;
    using Soulseek.NET;
    using Soulseek.NET.Tcp;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    class Program
    {
        public static string ActiveSearchText { get; set; }
        public static int ActiveSearchTicket { get; set; }

        static async Task Main(string[] args)
        {
            using (var client = new SoulseekClient())
            {
                client.Connection.StateChanged += Client_ServerStateChanged;
                client.SearchResultReceived += Client_SearchResultReceived;

                await client.ConnectAsync();

                Console.WriteLine("Enter username and password:");

                while (true)
                {
                    var cmd = Console.ReadLine();

                    if (cmd == "stop")
                    {
                        client.Connection.Disconnect("User requested Disconnect");
                    }
                    if (cmd.StartsWith("search"))
                    {
                        ActiveSearchText = string.Join(' ', cmd.Split(' ').Skip(1));
                        ActiveSearchTicket = client.Search(ActiveSearchText);
                    }
                    else
                    {
                        var r = await client.LoginAsync(cmd.Split(' ')[0], cmd.Split(' ')[1]);

                        if (r.Succeeded)
                        {
                            Console.WriteLine("Login succeeded");
                            Console.WriteLine(JsonConvert.SerializeObject(r));
                            //break;
                        }
                        else
                        {
                            Console.WriteLine("Login failed");
                        }
                    }
                }
            }
        }

        private static void Client_SearchResultReceived(object sender, SearchResultReceivedEventArgs e)
        {
            //Console.WriteLine(JsonConvert.SerializeObject(e, Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter()));
            var t = string.Empty;

            if (e.Result.Ticket != ActiveSearchTicket)
            {
                t = $"Result for <unknown search> ({ActiveSearchTicket} != {e.Result.Ticket})";
            }
            else
            {
                t = $"Result for '{ActiveSearchText}' ({ActiveSearchTicket}): ";
            }

            foreach (var file in e.Result.Files)
            {
                Console.WriteLine($"{t}: {file.Filename}");
            }
        }

        private static void Client_ServerStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"Server state changed to {e.State} ({e.Message})");
        }
    }
}
