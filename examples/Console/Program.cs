namespace Console
{
    using Soulseek.NET;
    using Soulseek.NET.Tcp;
    using System;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new SoulseekClient();
            client.Connection.ConnectionStateChanged += Client_ServerStateChanged;

            client.ConnectAsync();

            Console.WriteLine("Enter password:");

            while (true)
            {
                var cmd = Console.ReadLine();
                if (await client.LoginAsync("username", cmd))
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

        private static void Client_ServerStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"Server state changed to {e.State}");
        }
    }
}
