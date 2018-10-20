namespace Console
{
    using Newtonsoft.Json;
    using Soulseek.NET;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new SoulseekClient();
            client.Connection.StateChanged += Client_ServerStateChanged;

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
                    await client.SearchAsync(string.Join(' ', cmd.Split(' ').Skip(1)));
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

        private static void Client_ServerStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"Server state changed to {e.State}");
        }
    }
}
