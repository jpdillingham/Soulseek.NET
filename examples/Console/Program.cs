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
                client.ConnectionStateChanged += Client_ServerStateChanged;

                await client.ConnectAsync();

                Console.WriteLine("Enter username and password:");

                while (true)
                {
                    var cmd = Console.ReadLine();

                    if (cmd == "stop")
                    {
                        client.Disconnect("User requested Disconnect");
                    }
                    if (cmd.StartsWith("search"))
                    {
                        ActiveSearchText = string.Join(' ', cmd.Split(' ').Skip(1));

                        var search = client.CreateSearch(ActiveSearchText);
                        //search.SearchResultReceived += Client_SearchResultReceived;

                        ActiveSearchTicket = search.Ticket;
                        var result = await client.SearchAsync(ActiveSearchText);
                        Console.WriteLine($"Search complete.  {result.Results.Count()}");
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
                t = $"<unknown search> ({ActiveSearchTicket} != {e.Result.Ticket})";
            }
            else
            {
                t = $"'{ActiveSearchText}' ({ActiveSearchTicket}): ";
            }

            Console.WriteLine($"[SEARCH] {t} {e.Result.FileCount} results from {e.Result.Username}");

            //foreach (var file in e.Result.Files)
            //{
            //    //Console.WriteLine($"{t}: {file.Filename}");
            //}
        }

        private static void Client_ServerStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"Server state changed to {e.State} ({e.Message})");
        }
    }
}
