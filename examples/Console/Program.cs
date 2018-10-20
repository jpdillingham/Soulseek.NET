namespace Console
{
    using Soulseek.NET;
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
                    if (await client.LoginAsync(cmd.Split(' ')[0], cmd.Split(' ')[1]))
                    {
                        Console.WriteLine("Login succeeded");
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
