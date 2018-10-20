namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Generic;
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
        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();
        private List<Connection> PeerConnections { get; set; } = new List<Connection>();
        
        public async Task ConnectAsync()
        {
            await Connection.ConnectAsync();
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var request = new LoginRequest(username, password);

            var login = MessageWaiter.WaitFor(MessageCode.ServerLogin).Task;
            var roomList = MessageWaiter.WaitFor(MessageCode.ServerRoomList).Task;
            var privilegedUsers = MessageWaiter.WaitFor(MessageCode.ServerPrivilegedUsers).Task;

            await Connection.SendAsync(request.ToMessage().ToByteArray());

            Task.WaitAll(login, roomList, privilegedUsers);

            var response = (LoginResponse)login.Result;
            response.Rooms = ((RoomListResponse)roomList.Result).Rooms;
            response.PrivilegedUsers = ((PrivilegedUsersResponse)privilegedUsers.Result).PrivilegedUsers;

            return response;
        }

        public async Task SearchAsync(string searchText)
        {
            var request = new SearchRequest(searchText, 1);
            Console.WriteLine($"Searching for {searchText}...");
            await Connection.SendAsync(request.ToMessage().ToByteArray());
        }

        private void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Task.Run(() => DataReceived?.Invoke(this, e)).Forget();
            Task.Run(() => OnMessageReceived(new Message(e.Data))).Forget();
        }

        private async void OnMessageReceived(Message message)
        {
            var response = new object();
            var maps = MessageMapper.Map(message);

            switch (message.Code)
            {
                case MessageCode.ServerLogin:
                    response = new LoginResponse().MapFrom(message);
                    MessageWaiter.Complete(MessageCode.ServerLogin, response);
                    break;
                case MessageCode.ServerRoomList:
                    response = new RoomListResponse().MapFrom(message);
                    MessageWaiter.Complete(MessageCode.ServerRoomList, response);
                    break;
                case MessageCode.ServerPrivilegedUsers:
                    response = new PrivilegedUsersResponse().MapFrom(message);
                    MessageWaiter.Complete(MessageCode.ServerPrivilegedUsers, response);
                    break;
                case MessageCode.ServerConnectToPeer:
                    response = new ConnectToPeerResponse().MapFrom(message);
                    break;
                case MessageCode.PeerSearchReply:
                    Console.WriteLine("================================================================================================");
                    break;
                default:
                    Console.WriteLine($"Message Received: Code: {message.Code}");
                    response = null;
                    break;
            }

            MessageReceived?.Invoke(this, new MessageReceivedEventArgs() { Code = message.Code, Response = response });

            //if (response is RoomListResponse rls)
            //{
            //    foreach (var room in rls.Rooms)
            //    {
            //        Console.WriteLine($"Room: {room.Name}, Users: {room.UserCount}");
            //    }
            //}

            //if (response is PrivilegedUsersResponse pu)
            //{
            //    foreach (var u in pu.PrivilegedUsers)
            //    {
            //        Console.WriteLine($"Privileged User: {u}");
            //    }
            //}

            if (response is ConnectToPeerResponse c)
            {
                //Console.WriteLine($"\tUsername: {c.Username}, Type: {c.Type}, IP: {c.IPAddress}, Port: {c.Port}, Token: {c.Token}");

                var connection = new Connection(ConnectionType.Peer, c.IPAddress.ToString(), c.Port);
                PeerConnections.Add(connection);

                connection.DataReceived += OnConnectionDataReceived;
                //connection.StateChanged += OnPeerConnectionStateChanged;

                await connection.ConnectAsync();
                //Console.WriteLine($"\tConnection to {c.Username} opened.");

                var request = new PierceFirewallRequest(c.Token);
                await connection.SendAsync(request.ToByteArray(), suppressCodeNormalization: true);
            }
        }

        private void OnPeerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"\tPeer Connection State Changed: {e.State} ({e.Message ?? "Unknown"})");
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"Connection State Changed: {e.State} ({e.Message ?? "Unknown"})");
            ConnectionStateChanged?.Invoke(this, e);
        }
    }
}