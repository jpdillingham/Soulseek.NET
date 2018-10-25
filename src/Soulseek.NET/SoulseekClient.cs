namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using SystemTimer = System.Timers.Timer;

    public class SoulseekClient : IDisposable
    {
        public SoulseekClient(string address = "server.slsknet.org", int port = 2242)
        {
            Address = address;
            Port = port;

            Connection = new Connection(ConnectionType.Server, Address, Port);
            Connection.StateChanged += OnServerConnectionStateChanged;
            Connection.DataReceived += OnConnectionDataReceived;

            PeerConnectionMonitor = new SystemTimer(5000);
            PeerConnectionMonitor.Elapsed += PeerConnectionMonitor_Elapsed;
        }

        private void PeerConnectionMonitor_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var total = 0;
                var connecting = 0;
                var connected = 0;

                PeerConnectionsLock.EnterUpgradeableReadLock();

                try
                {
                    total = PeerConnections.Count();
                    connecting = PeerConnections.Where(c => c?.State == ConnectionState.Connecting).Count();
                    connected = PeerConnections.Where(c => c?.State == ConnectionState.Connected).Count();
                    var disconnectedPeers = new List<Connection>(PeerConnections.Where(c => c == null || c.State == ConnectionState.Disconnected));

                    Console.WriteLine($"████████████████████ Peers: Total: {total}, Connecting: {connecting}, Connected: {connected}, Disconnected: {disconnectedPeers.Count()}");
                
                    PeerConnectionsLock.EnterWriteLock();

                    try
                    {
                        foreach (var connection in disconnectedPeers)
                        {
                            connection?.Dispose();
                            PeerConnections.Remove(connection);
                        }
                    }
                    finally
                    {
                        PeerConnectionsLock.ExitWriteLock();
                    }
                }
                finally
                {
                    PeerConnectionsLock.ExitUpgradeableReadLock();
                }

                PeerConnectionMonitor.Reset();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in peer connection monitor: {ex}");
            }
        }

        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;
        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public string Address { get; private set; }
        public int ParentMinSpeed { get; private set; }
        public int ParentSpeedRatio { get; private set; }
        public int Port { get; private set; }

        public IEnumerable<string> PrivilegedUsers { get; private set; }
        public IEnumerable<Room> Rooms { get; private set; }
        public int WishlistInterval { get; private set; }
        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();

        private Connection Connection { get; set; }
        private List<Connection> PeerConnections { get; set; } = new List<Connection>();
        private SystemTimer PeerConnectionMonitor { get; set; }
        private bool Disposed { get; set; } = false;
        private Random Random { get; set; } = new Random();
        private ReaderWriterLockSlim PeerConnectionsLock { get; set; } = new ReaderWriterLockSlim();

        private List<Search> ActiveSearches { get; set; } = new List<Search>();

        public async Task ConnectAsync()
        {
            await Connection.ConnectAsync();
        }

        public void Disconnect(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "User initiated shutdown";
            }

            Connection.Disconnect(message);

            foreach (var connection in PeerConnections)
            {
                connection.Disconnect(message);
            }
        }

        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            try
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

                Rooms = (IEnumerable<Room>)roomList.Result;
                ParentMinSpeed = ((int)parentMinSpeed.Result);
                ParentSpeedRatio = ((int)parentSpeedRatio.Result);
                WishlistInterval = ((int)wishlistInterval.Result);
                PrivilegedUsers = (IEnumerable<string>)privilegedUsers.Result;

                return (LoginResponse)login.Result;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                return null;
            }
        }

        public Search CreateSearch(string searchText)
        {
            var search = new Search(Connection, searchText);
            ActiveSearches.Add(search);

            // do not start, to give the client time to bind event handlers
            //search.Start();

            return search;
        }

        public async Task<Search> SearchAsync(string searchText)
        {
            //todo: create and execute search, spin until it is complete, return results
            return null;
        }

        private async Task HandleServerConnectToPeer(ConnectToPeerResponse response, NetworkEventArgs e)
        {
            var connection = new Connection(ConnectionType.Peer, response.IPAddress.ToString(), response.Port);
            connection.DataReceived += OnConnectionDataReceived;
            connection.StateChanged += OnPeerConnectionStateChanged;

            PeerConnectionsLock.EnterWriteLock();

            try
            {
                PeerConnections.Add(connection);
            }
            finally
            {
                PeerConnectionsLock.ExitWriteLock();
            }

            try
            {
                await connection.ConnectAsync();

                var request = new PierceFirewallRequest(response.Token);
                await connection.SendAsync(request.ToByteArray(), suppressCodeNormalization: true);
            }
            catch (ConnectionException ex)
            {
                connection.Disconnect($"Failed to connect to peer {response.Username}@{response.IPAddress}:{response.Port}: {ex.Message}");
            }
        }

        private async Task HandlePeerSearchReply(SearchResponse response, NetworkEventArgs e)
        {
            if (response.FileCount > 0)
            {
                var search = ActiveSearches.Where(s => s.Ticket == response.Ticket).SingleOrDefault();

                if (search != default(Search))
                {
                    search.AddResult(new SearchResultReceivedEventArgs(e) { Result = response });
                }
            }
        }

        private async Task HandlePrivateMessage(PrivateMessage message, NetworkEventArgs e)
        {
            Console.WriteLine($"[{message.Timestamp}][{message.Username}]: {message.Message}");
            await Connection.SendAsync(new AcknowledgePrivateMessageRequest(message.Id).ToByteArray());
        }

        private async void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            //Console.WriteLine($"Data received: {e.Data.Length} bytes");
            Task.Run(() => DataReceived?.Invoke(this, e)).Forget();
            
            var message = new Message(e.Data);
            var messageEventArgs = new MessageReceivedEventArgs(e) { Message = message };

            //Console.WriteLine($"Message receiveD: {message.Code}, {message.Payload.Length} bytes");
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
                case MessageCode.ServerPrivateMessages:
                    await HandlePrivateMessage(PrivateMessage.Parse(message), e);
                    break;
                default:
                    Console.WriteLine($"Unknown message: {message.Code}: {message.Payload.Length} bytes");
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
            Console.WriteLine($"Dispose?");
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