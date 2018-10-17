using Soulseek.NET.Messaging;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Soulseek.NET
{
    public class Client
    {
        private Socket Server { get; set; }

        public string Address { get; private set; }
        public int Port { get; private set; }
        public ServerState State { get; private set; } = ServerState.Disconnected;

        public event EventHandler<ServerStateChangedEventArgs> ServerStateChanged;

        public Client(string address = "server.slsknet.org", int port = 2242)
        {
            Address = address;
            Port = port;
        }

        public void Connect()
        {
            var host = Dns.GetHostEntry(Address);
            var ip = host.AddressList[0];

            Server = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            ChangeServerState(ServerState.Connecting);

            try
            {
                Server.Connect(ip, Port);
                ChangeServerState(ServerState.Connected);
            }
            catch (Exception ex)
            {
                ChangeServerState(ServerState.Disconnected);
                throw new ServerException($"Failed to connect to {Address}:{Port}: {ex.Message}", ex);
            }
        }

        public Task ConnectAsync()
        {
            return Task.Run(() => Connect());
        }

        private void ChangeServerState(ServerState state)
        {
            State = state;
            ServerStateChanged?.Invoke(this, new ServerStateChangedEventArgs() { State = state });
        }

        public Task<bool> LoginAsync(string username, string password)
        {
            return Task.Run(() => Login(username, password));
        }

        public bool Login(string username, string password)
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

            // Send the data through the socket.  
            int bytesSent = Server.Send(request);

            byte[] bytes = new byte[4096];
            int bytesRec = Server.Receive(bytes);

            var reader = new MessageReader(bytes);
            Console.WriteLine($"Length: {reader.Length()}");
            Console.WriteLine($"Code: {reader.Code()}");

            var result = reader.ReadByte();
            Console.WriteLine($"Result: {result}");
            Console.WriteLine($"Message: {reader.ReadString()}");

            return result == 1;
        }
    }
}
