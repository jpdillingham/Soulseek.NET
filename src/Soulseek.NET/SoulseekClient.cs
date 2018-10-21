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
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<SearchResultReceivedEventArgs> SearchResultReceived;

        public string Address { get; private set; }
        public Connection Connection { get; private set; }
        public int Port { get; private set; }

        public IEnumerable<Room> Rooms { get; private set; }
        public int ParentMinSpeed { get; private set; }
        public int ParentSpeedRatio { get; private set; }
        public int WishlistInterval { get; private set; }
        public IEnumerable<string> PrivilegedUsers { get; private set; }

        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();

        private List<Connection> PeerConnections { get; set; } = new List<Connection>();
        
        public async Task ConnectAsync()
        {
            await Connection.ConnectAsync();
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var request = new LoginRequest(username, password);

            var login = MessageWaiter.Wait(MessageCode.ServerLogin).Task;
            var roomList = MessageWaiter.Wait(MessageCode.ServerRoomList).Task;
            var parentMinSpeed = MessageWaiter.Wait(MessageCode.ServerParentMinSpeed).Task;
            var parentSpeedRatio = MessageWaiter.Wait(MessageCode.ServerParentSpeedRatio).Task;
            var wishlistInterval = MessageWaiter.Wait(MessageCode.ServerWishlistInterval).Task;
            var privilegedUsers = MessageWaiter.Wait(MessageCode.ServerPrivilegedUsers).Task;

            await Connection.SendAsync(request.ToMessage().ToByteArray());

            Task.WaitAll(login, roomList, parentMinSpeed, parentSpeedRatio, wishlistInterval, privilegedUsers);

            Rooms = ((RoomListResponse)roomList.Result).Rooms;
            ParentMinSpeed = ((IntegerResponse)parentMinSpeed.Result).Value;
            ParentSpeedRatio = ((IntegerResponse)parentSpeedRatio.Result).Value;
            WishlistInterval = ((IntegerResponse)wishlistInterval.Result).Value;
            PrivilegedUsers = ((PrivilegedUsersResponse)privilegedUsers.Result).PrivilegedUsers;

            return (LoginResponse)login.Result;
        }

        public async Task SearchAsync(string searchText)
        {
            var request = new SearchRequest(searchText, 1);
            Console.WriteLine($"Searching for {searchText}...");
            await Connection.SendAsync(request.ToMessage().ToByteArray());
        }

        private void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            //Console.WriteLine($"Data Received: {e.Data.Length} bytes");
            Task.Run(() => DataReceived?.Invoke(this, e)).Forget();
            Task.Run(() => OnMessageReceived(new Message(e.Data))).Forget();
        }

        private async void OnMessageReceived(Message message)
        {
            object response = null;

            if (new MessageMapper().TryMapResponse(message, out var mappedResponse))
            {
                MessageWaiter.Complete(message.Code, mappedResponse);
            }
            else
            {
                Console.WriteLine($"No Mapping for Code: {message.Code}");

                switch (message.Code)
                {
                    case MessageCode.ServerConnectToPeer:
                        Console.WriteLine("+++++++++++++++++++++++");
                        response = new ConnectToPeerResponse().Map(message);
                        break;
                    case MessageCode.PeerSearchReply:
                        Console.WriteLine("================================================================================================");
                        break;
                    default:
                        Console.WriteLine($"Message Received: Code: {message.Code}");
                        response = null;
                        break;
                }
            }

            MessageReceived?.Invoke(this, new MessageReceivedEventArgs() { Code = message.Code, Response = mappedResponse ?? response });

            if (mappedResponse is ConnectToPeerResponse connectToPeerResponse)
            {
                await HandleConnectToPeerResponse(connectToPeerResponse);
            }

            if (mappedResponse is PeerSearchReplyResponse peerSearchReplyResponse)
            {
                if (peerSearchReplyResponse.FileCount > 0)
                {
                    //Console.WriteLine($"Search result recieved from {peerSearchReplyResponse.Username}");
                    Task.Run(() => SearchResultReceived?.Invoke(this, new SearchResultReceivedEventArgs() { Response = peerSearchReplyResponse })).Forget();
                }
            }
        }

        private async Task HandleConnectToPeerResponse(ConnectToPeerResponse connectToPeerResponse)
        {
            var connection = new Connection(ConnectionType.Peer, connectToPeerResponse.IPAddress.ToString(), connectToPeerResponse.Port);
            PeerConnections.Add(connection);

            connection.DataReceived += OnConnectionDataReceived;
            connection.StateChanged += OnPeerConnectionStateChanged;

            try
            {
                await connection.ConnectAsync();

                var request = new PierceFirewallRequest(connectToPeerResponse.Token);
                await connection.SendAsync(request.ToByteArray(), suppressCodeNormalization: true);
            }
            catch (ConnectionException ex)
            {
                Console.WriteLine($"Failed to connect to Peer {connectToPeerResponse.Username}@{connectToPeerResponse.IPAddress}: {ex.Message}");
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