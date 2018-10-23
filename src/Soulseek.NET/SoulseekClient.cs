namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;

    public class SoulseekClient : IDisposable
    {
        public SoulseekClient(string address = "server.slsknet.org", int port = 2242)
        {
            Address = address;
            Port = port;

            Connection = new Connection(ConnectionType.Server, Address, Port);
            Connection.StateChanged += OnServerConnectionStateChanged;
            Connection.DataReceived += OnConnectionDataReceived;

            PeerConnectionMonitor.Elapsed += PeerConnectionMonitor_Elapsed;
        }

        private void PeerConnectionMonitor_Elapsed(object sender, ElapsedEventArgs e)
        {
            var total = PeerConnections.Count();
            var connecting = PeerConnections.Where(c => c.State == ConnectionState.Connecting).Count();
            var connected = PeerConnections.Where(c => c.State == ConnectionState.Connected).Count();
            var disconnecting = PeerConnections.Where(c => c.State == ConnectionState.Disconnecting).Count();

            foreach (var connection in PeerConnections.Where(c => c.State == ConnectionState.Disconnected))
            {
                connection.Dispose();
                PeerConnections.Remove(connection);
            }

            Console.WriteLine($"Peers: Total: {total}, Connecting: {connecting}, Connected: {connected}, Disconnecting: {disconnecting}");
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<SearchResultReceivedEventArgs> SearchResultReceived;
        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

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
        private Timer PeerConnectionMonitor { get; set; } = new Timer(1000);
        private bool Disposed { get; set; } = false;
        private Random Random { get; set; } = new Random();

        public async Task ConnectAsync()
        {
            await Connection.ConnectAsync();
        }

        public void Disconnect()
        {
            Connection.Disconnect("User initiated shutdown");

            foreach (var connection in PeerConnections)
            {
                connection.Disconnect("User initiated shutdown");
            }
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

            Rooms = ((RoomList)roomList.Result).Rooms;
            ParentMinSpeed = ((Integer)parentMinSpeed.Result).Value;
            ParentSpeedRatio = ((Integer)parentSpeedRatio.Result).Value;
            WishlistInterval = ((Integer)wishlistInterval.Result).Value;
            PrivilegedUsers = ((PrivilegedUserList)privilegedUsers.Result).PrivilegedUsers;

            return (LoginResponse)login.Result;
        }

        public int Search(string searchText)
        {
            return Search(searchText, Random.Next(1, 2147483647));
        }

        public int Search(string searchText, int ticket)
        {
            var request = new SearchRequest(searchText, ticket);
            Console.WriteLine($"Searching for {searchText}...");
            Task.Run(() => Connection.SendAsync(request.ToMessage().ToByteArray())).GetAwaiter().GetResult();

            return ticket;
        }

        private async Task HandleServerConnectToPeer(ConnectToPeerResponse response, NetworkEventArgs e)
        {
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
                //Console.WriteLine($"Failed to connect to Peer {response.Username}@{response.IPAddress}: {ex.Message}");
            }
        }

        private async Task HandlePeerSearchReply(SearchResponse response, NetworkEventArgs e)
        {
            if (response.FileCount > 0)
            {
                await Task.Run(() => SearchResultReceived?.Invoke(this, new SearchResultReceivedEventArgs(e)
                {
                    Result = response,
                }));
            }
        }

        private async void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Task.Run(() => DataReceived?.Invoke(this, e)).Forget();
            
            var message = new Message(e.Data);

            var messageEventArgs = new MessageReceivedEventArgs(e)
            {
                Message = message,
            };

            Task.Run(() => MessageReceived?.Invoke(this, messageEventArgs)).Forget();

            switch (message.Code)
            {
                case MessageCode.ServerParentMinSpeed:
                case MessageCode.ServerParentSpeedRatio:
                case MessageCode.ServerWishlistInterval:
                    MessageWaiter.Complete(message.Code, Integer.Parse(message));
                    break;
                case MessageCode.ServerLogin:
                    MessageWaiter.Complete(message.Code, LoginResponse.Parse(message));
                    break;
                case MessageCode.ServerRoomList:
                    MessageWaiter.Complete(message.Code, RoomList.Parse(message));
                    break;
                case MessageCode.ServerPrivilegedUsers:
                    MessageWaiter.Complete(message.Code, PrivilegedUserList.Parse(message));
                    break;
                case MessageCode.PeerSearchReply:
                    await HandlePeerSearchReply(SearchResponse.Parse(message), e);
                    break;
                case MessageCode.ServerConnectToPeer:
                    await HandleServerConnectToPeer(ConnectToPeerResponse.Parse(message), e);
                    break;
                default:
                    break;
            }
        }

        private async void OnServerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Connected)
            {
                PeerConnectionMonitor.Start();
            }

            await Task.Run(() => ConnectionStateChanged?.Invoke(this, e));
        }

        private void OnPeerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Disconnected && sender is Connection connection)
            {
                connection.Dispose();
                PeerConnections.Remove(connection);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Connection?.Dispose();
                    PeerConnections?.ForEach(c => c.Dispose());
                    PeerConnectionMonitor?.Dispose();
                }

                Disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}