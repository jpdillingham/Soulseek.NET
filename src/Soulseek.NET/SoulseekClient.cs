namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Maps;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Tcp;
    using System;
    using System.Threading.Tasks;

    public class SoulseekClient
    {
        public SoulseekClient(string address = "server.slsknet.org", int port = 2242)
        {
            Address = address;
            Port = port;

            Connection = new Connection(ConnectionType.Server, Address, Port);
            Connection.StateChanged += OnConnectionStateChanged;
            Connection.DataReceived += OnConnectionDataReceived;

            MessageMapper = new MessageMapper();
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public string Address { get; private set; }
        public Connection Connection { get; private set; }
        public int Port { get; private set; }

        private MessageMapper MessageMapper { get; set; }

        public async Task ConnectAsync()
        {
            await Connection.ConnectAsync();
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var request = new LoginRequest(username, password);

            Console.WriteLine($"Logging in as {username}...");

            await Connection.SendAsync(request.ToMessage().ToByteArray());

            return true;
        }

        public async Task SearchAsync(string searchText)
        {
            var request = new SearchRequest(searchText, 1);
            Console.WriteLine($"Searching for {searchText}...");
            await Connection.SendAsync(request.ToMessage().ToByteArray());
        }

        private void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"Data Received: {e.Data.Length} bytes");
            DataReceived?.Invoke(this, e);

            OnMessageReceived(new Message(e.Data));
        }

        private void OnMessageReceived(Message message)
        {
            var response = new object();
            var maps = MessageMapper.Map(message);

            Console.WriteLine($"Message Received: Code: {message.Code}");

            switch (message.Code)
            {
                case MessageCode.ServerLogin:
                    response = new LoginResponse().MapFrom(message);
                    break;
                case MessageCode.ServerRoomList:
                    response = new RoomListResponse().MapFrom(message);
                    break;
                case MessageCode.ServerPrivilegedUsers:
                    response = new PrivilegedUsersResponse().MapFrom(message);
                    break;
                case MessageCode.ServerConnectToPeer:
                    response = new ConnectToPeerResponse().MapFrom(message);
                    break;
                default:
                    response = null;
                    break;
            }

            MessageReceived?.Invoke(this, new MessageReceivedEventArgs() { Code = message.Code, Response = response });

            if (response is LoginResponse lr)
            {
                Console.WriteLine($"Status: {lr.Status}");
                Console.WriteLine($"Message: {lr.Message}");
                Console.WriteLine($"IPAddress: {lr.IPAddress}");
            }

            if (response is RoomListResponse rls)
            {
                foreach (var room in rls.Rooms)
                {
                    Console.WriteLine($"Room: {room.Name}, Users: {room.UserCount}");
                }
            }

            if (response is PrivilegedUsersResponse pu)
            {
                foreach (var u in pu.PrivilegedUsers)
                {
                    Console.WriteLine($"Privileged User: {u}");
                }
            }

            if (response is ConnectToPeerResponse c)
            {
                Console.WriteLine($"Username: {c.Username}, Type: {c.Type}, IP: {c.IPAddress}, Port: {c.Port}, Token: {c.Token}");
            }
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"Connection State Changed: {e.State} ({e.Message ?? "Unknown"})");
            ConnectionStateChanged?.Invoke(this, e);
        }
    }
}