namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Tcp;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class Client
    {
        public Client(string address = "server.slsknet.org", int port = 2242)
        {
            Address = address;
            Port = port;

            Connection = new Connection(Address, Port);
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ServerStateChanged;

        public string Address { get; private set; }
        public Connection Connection { get; private set; }
        public int Port { get; private set; }

        public async Task ConnectAsync()
        {
            await Connection.ConnectAsync();
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var request = new MessageBuilder()
                .Code(MessageCode.Login)
                .WriteString(username)
                .WriteString(password)
                .WriteInteger(181)
                .WriteString($"{username}{password}".ToMD5Hash())
                .WriteInteger(1)
                .Build();

            Console.WriteLine($"Logging in as {username}...");

            await Connection.WriteAsync(request);
            var responses = await Connection.ReadAsync();

            foreach (var response in responses)
            {
                var rdr = new MessageReader(response);
                Console.WriteLine($"Length: {rdr.Length()}, Code: {rdr.Code()}");
            }

            Console.WriteLine("----------");
            var reader = new MessageReader(responses.ToArray()[0]);
            Console.WriteLine($"Length: {reader.Length()}");
            Console.WriteLine($"Code: {reader.Code()}");

            var result = reader.ReadByte();
            Console.WriteLine($"Result: {result}");
            Console.WriteLine($"Message: {reader.ReadString()}");

            var success = result == 1;
            return success;
        }
    }
}
