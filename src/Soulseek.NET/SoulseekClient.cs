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
        public event EventHandler<ResponseReceivedEventArgs> ResponseReceived;
        public event EventHandler<SearchResultReceivedEventArgs> SearchResultReceived;
        public event EventHandler<MessageReceivedEventArgs> UnknownMessageRecieved;

        public string Address { get; private set; }
        public Connection Connection { get; private set; }
        public int ParentMinSpeed { get; private set; }
        public int ParentSpeedRatio { get; private set; }
        public int Port { get; private set; }

        public IEnumerable<string> PrivilegedUsers { get; private set; }
        public IEnumerable<Room> Rooms { get; private set; }
        public int WishlistInterval { get; private set; }
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

        private async Task HandleServerConnectToPeer(Message message)
        {
            var response = ServerConnectToPeerResponse.Map(message);
            var connection = new Connection(ConnectionType.Peer, response.IPAddress.ToString(), response.Port);
            PeerConnections.Add(connection);

            connection.DataReceived += OnConnectionDataReceived;
            connection.StateChanged += OnPeerConnectionStateChanged;

            try
            {
                await connection.ConnectAsync();

                var request = new PierceFirewallRequest(response.Token);
                await connection.SendAsync(request.ToByteArray(), suppressCodeNormalization: true);
            }
            catch (ConnectionException ex)
            {
                Console.WriteLine($"Failed to connect to Peer {response.Username}@{response.IPAddress}: {ex.Message}");
            }
        }

        private async Task HandlePeerSearchReply(Message message)
        {
            var response = PeerSearchReplyResponse.Map(message);

            if (response.FileCount > 0)
            {
                var eventArgs = new SearchResultReceivedEventArgs() { Response = response };
                await Task.Run(() => SearchResultReceived?.Invoke(this, eventArgs));
            }
        }

        private async void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Task.Run(() => DataReceived?.Invoke(this, e)).Forget();
            
            var message = new Message(e.Data);
            Task.Run(() => MessageReceived?.Invoke(this, new MessageReceivedEventArgs() { Message = message })).Forget();

            switch (message.Code)
            {
                case MessageCode.ServerParentMinSpeed:
                case MessageCode.ServerParentSpeedRatio:
                case MessageCode.ServerWishlistInterval:
                    MessageWaiter.Complete(message.Code, IntegerResponse.Map(message));
                    break;
                case MessageCode.ServerLogin:
                    MessageWaiter.Complete(message.Code, LoginResponse.Map(message));
                    break;
                case MessageCode.ServerRoomList:
                    MessageWaiter.Complete(message.Code, RoomListResponse.Map(message));
                    break;
                case MessageCode.ServerPrivilegedUsers:
                    MessageWaiter.Complete(message.Code, PrivilegedUsersResponse.Map(message));
                    break;
                case MessageCode.PeerSearchReply:
                    await HandlePeerSearchReply(message);
                    break;
                case MessageCode.ServerConnectToPeer:
                    await HandleServerConnectToPeer(message);
                    break;
                default:
                    Task.Run(() => UnknownMessageRecieved?.Invoke(this, new MessageReceivedEventArgs() { Message = message })).Forget();
                    break;
            }

            //if (new MessageMapper().TryMapResponse(message, out var response))
            //{
            //    MessageWaiter.Complete(message.Code, response);

            //    var eventArgs = new ResponseReceivedEventArgs()
            //    {
            //        Message = message,
            //        ResponseType = response.GetType(),
            //        Response = response,
            //    };

            //    Task.Run(() => ResponseReceived?.Invoke(this, eventArgs)).Forget();

            //    if (MessageHandler.TryGetHandler(this, message.Code, out var handler))
            //    {
            //        Console.WriteLine($"Handler for {message.Code}: {handler.Method.Name}");
            //        await handler.Invoke(this, message, response);
            //    }
            //    else
            //    {
            //        Console.WriteLine($"Failed to find handler for {message.Code}");
            //    }
            //    //await HandleMessage(response);
            //}
            //else
            //{
                
            //}
        }

        private async void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            await Task.Run(() => ConnectionStateChanged?.Invoke(this, e));
        }

        private void OnPeerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"\tPeer Connection State Changed: {e.State} ({e.Message ?? "Unknown"})");
        }
    }
}