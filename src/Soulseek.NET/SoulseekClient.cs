// <copyright file="SoulseekClient.cs" company="JP Dillingham">
//     Copyright(C) 2018 JP Dillingham
//     
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//     
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//     GNU General Public License for more details.
//     
//     You should have received a copy of the GNU General Public License
//     along with this program.If not, see<https://www.gnu.org/licenses/>.
// </copyright>

namespace Soulseek.NET
{
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Timers;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class with the specified <paramref name="address"/> and <paramref name="port"/>.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        public SoulseekClient(string address = "server.slsknet.org", int port = 2242, int concurrentPeerConnectionLimit = 500)
        {
            Address = address;
            Port = port;
            ConcurrentPeerConnectionLimit = concurrentPeerConnectionLimit;

            Connection = new Connection(ConnectionType.Server, Address, Port);
            Connection.StateChanged += OnServerConnectionStateChanged;
            Connection.DataReceived += OnConnectionDataReceived;

            PeerConnectionsMonitorTimer = new SystemTimer(5000);
            PeerConnectionsMonitorTimer.Elapsed += OnPeerConnectionMonitorTick;
        }

        /// <summary>
        ///     Occurs when the underlying TCP connection to the server changes state.
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        ///     Occurs when raw data is recieved by the underlying TCP connection.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        ///     Occurs when a new message is recieved.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     Gets or sets the address of the server to which to connect.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        ///     Gets or sets the limit to the number of concurrent peer connections.
        /// </summary>
        public int ConcurrentPeerConnectionLimit { get; set; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        public ConnectionState ConnectionState => Connection.State;

        /// <summary>
        ///     Gets the ParentMinSpeed value from the server.
        /// </summary>
        public int ParentMinSpeed { get; private set; }

        /// <summary>
        ///     Gets the ParentSpeedRatio value from the server.
        /// </summary>
        public int ParentSpeedRatio { get; private set; }

        /// <summary>
        ///     Gets or sets the port to which to connect.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     Gets the list of privileged users from the server.
        /// </summary>
        public IEnumerable<string> PrivilegedUsers { get; private set; }

        /// <summary>
        ///     Gets the list of rooms from the server.
        /// </summary>
        public IEnumerable<Room> Rooms { get; private set; }

        /// <summary>
        ///     Gets the WishlistInterval value from the server.
        /// </summary>
        public int WishlistInterval { get; private set; }

        private Connection Connection { get; set; }
        private bool Disposed { get; set; } = false;
        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();
        private Random Random { get; set; } = new Random();

        private ConcurrentDictionary<int, Search> SearchesActive { get; set; } = new ConcurrentDictionary<int, Search>();

        private SystemTimer PeerConnectionsMonitorTimer { get; set; }
        private ConcurrentQueue<KeyValuePair<ConnectToPeerResponse, Connection>> PeerConnectionsQueued { get; set; } = new ConcurrentQueue<KeyValuePair<ConnectToPeerResponse, Connection>>();
        private ConcurrentDictionary<ConnectToPeerResponse, Connection> PeerConnectionsActive { get; set; } = new ConcurrentDictionary<ConnectToPeerResponse, Connection>();

        /// <summary>
        ///     Connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        public void Connect()
        {
            Task.Run(() => ConnectAsync()).GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        public async Task ConnectAsync()
        {
            await Connection.ConnectAsync();
        }

        /// <summary>
        ///     Creates a Pending <see cref="Soulseek.Search"/> with the specified <paramref name="searchText"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <returns>A <see cref="SearchState.Pending"/> Search.</returns>
        public Search CreateSearch(string searchText)
        {
            var search = new Search(Connection, searchText);
            search.SearchCompleted += OnSearchCompleted;

            SearchesActive.TryAdd(search.Ticket, search);

            return search;
        }

        /// <summary>
        ///     Disconnects the client from the server with an optionally supplied <paramref name="message"/>.
        /// </summary>
        /// <param name="message">An optional disconnect message.</param>
        public void Disconnect(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "User initiated shutdown";
            }

            Connection.Disconnect(message);

            ClearPeerConnectionsQueued();
            ClearPeerConnectionsActive(message);
            ClearSearchesActive();
        }

        private void ClearSearchesActive()
        {
            foreach (var searchToRemove in SearchesActive.ToList())
            {
                if (SearchesActive.TryRemove(searchToRemove.Key, out var search))
                {
                    search.Stop();
                    search.Dispose();
                }
            }
        }

        private void ClearPeerConnectionsQueued()
        {
            while (!PeerConnectionsQueued.IsEmpty)
            {
                if (PeerConnectionsQueued.TryDequeue(out var queuedConnection))
                {
                    queuedConnection.Value.Dispose();
                }
            }
        }

        private void ClearPeerConnectionsActive(string disconnectMessage)
        {
            foreach (var connectionToRemove in PeerConnectionsActive.ToList())
            { 
                if (PeerConnectionsActive.TryRemove(connectionToRemove.Key, out var connection))
                {
                    connection.Disconnect(disconnectMessage);
                    connection.Dispose();
                }
            }
        }

        /// <summary>
        ///     Disposes this instance.s
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Logs in to the server with the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <returns>The server response.</returns>
        public LoginResponse Login(string username, string password)
        {
            return Task.Run(() => LoginAsync(username, password)).GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Asynchronously logs in to the server with the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <returns>The server response.</returns>
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

            Rooms = (IEnumerable<Room>)roomList.Result;
            ParentMinSpeed = ((int)parentMinSpeed.Result);
            ParentSpeedRatio = ((int)parentSpeedRatio.Result);
            WishlistInterval = ((int)wishlistInterval.Result);
            PrivilegedUsers = (IEnumerable<string>)privilegedUsers.Result;

            return (LoginResponse)login.Result;
        }

        /// <summary>
        ///     Performs a search for the specified <paramref name="searchText"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <returns>The completed search result.</returns>
        public Search Search(string searchText)
        {
            return Task.Run(() => SearchAsync(searchText)).GetAwaiter().GetResult();
        }

        /// <summary>
        ///     Asynchronously performs a search for the specified <paramref name="searchText"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <returns>The completed search result.</returns>
        public async Task<Search> SearchAsync(string searchText)
        {
            var search = CreateSearch(searchText);

            await search.StartAsync();

            var result = await MessageWaiter.Wait(MessageCode.ServerFileSearch, search.Ticket).Task;

            return (Search)result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    var message = "Client is being disposed.";

                    Connection?.Disconnect(message);
                    Connection?.Dispose();

                    ClearPeerConnectionsQueued();
                    ClearPeerConnectionsActive(message);
                    ClearSearchesActive();

                    PeerConnectionsMonitorTimer?.Dispose();
                }

                Disposed = true;
            }
        }

        private void HandlePeerSearchResponse(SearchResponse response, NetworkEventArgs e)
        {
            if (response != null && response.FileCount > 1 && response.InQueue == 0 && response.FreeUploadSlots > 0)
            {
                if (SearchesActive.TryGetValue(response.Ticket, out var search) && search.State == SearchState.InProgress)
                {
                    search.AddResponse(response, e);
                }
            }
        }

        private async Task HandlePrivateMessage(PrivateMessage message, NetworkEventArgs e)
        {
            Console.WriteLine($"[{message.Timestamp}][{message.Username}]: {message.Message}");
            await Connection.SendAsync(new AcknowledgePrivateMessageRequest(message.Id).ToByteArray());
        }

        private async Task HandleServerConnectToPeer(ConnectToPeerResponse response, NetworkEventArgs e)
        {
            var connection = new Connection(ConnectionType.Peer, response.IPAddress.ToString(), response.Port);
            connection.Context = response;
            connection.DataReceived += OnPeerConnectionDataReceived;
            connection.StateChanged += OnPeerConnectionStateChanged;
            
            if (PeerConnectionsActive.Count() < ConcurrentPeerConnectionLimit - 1)
            {
                if (PeerConnectionsActive.TryAdd(response, connection))
                {
                    await TryConnectPeerConnection(response, connection);
                }
            }
            else
            {
                PeerConnectionsQueued.Enqueue(new KeyValuePair<ConnectToPeerResponse, Connection>(response, connection));
            }
        }

        private async Task TryConnectPeerConnection(ConnectToPeerResponse response, Connection connection)
        {
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

        private async void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Task.Run(() => DataReceived?.Invoke(this, e)).Forget();

            var message = new Message(e.Data);
            var messageEventArgs = new MessageReceivedEventArgs(e) { Message = message };

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

                case MessageCode.ServerConnectToPeer:
                    await HandleServerConnectToPeer(ConnectToPeerResponse.Parse(message), e);
                    break;

                case MessageCode.ServerPrivateMessages:
                    await HandlePrivateMessage(PrivateMessage.Parse(message), e);
                    break;

                default:
                    Console.WriteLine($"Unknown message: [{e.IPAddress}] {message.Code}: {message.Payload.Length} bytes");
                    break;
            }
        }

        private void OnPeerConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            var message = new Message(e.Data);

            switch (message.Code)
            {
                case MessageCode.PeerSearchResponse:
                    HandlePeerSearchResponse(SearchResponse.Parse(message), e);
                    break;
                default:
                    if (sender is Connection peerConnection)
                    {
                        peerConnection.Disconnect($"Unknown response from peer: {message.Code}");
                    }
                    break;
            }
        }

        private async void OnPeerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Disconnected && 
                sender is Connection connection && 
                connection.Context is ConnectToPeerResponse connectToPeerResponse)
            {
                connection.Dispose();
                PeerConnectionsActive.TryRemove(connectToPeerResponse, out var _);

                if (PeerConnectionsQueued.TryDequeue(out var nextConnection))
                {
                    if (PeerConnectionsActive.TryAdd(nextConnection.Key, nextConnection.Value))
                    {
                        await TryConnectPeerConnection(nextConnection.Key, nextConnection.Value);
                    }
                }
            }
        }

        private void OnSearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            SearchesActive.TryRemove(e.Search.Ticket, out var removed);

            MessageWaiter.Complete(MessageCode.ServerFileSearch, e.Search.Ticket, e.Search);
        }

        private async void OnServerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Connected)
            {
                PeerConnectionsMonitorTimer.Start();
            }

            await Task.Run(() => ConnectionStateChanged?.Invoke(this, e));
        }

        private void OnPeerConnectionMonitorTick(object sender, ElapsedEventArgs e)
        {
            var total = PeerConnectionsActive.Count();
            var connecting = PeerConnectionsActive.Where(c => c.Value?.State == ConnectionState.Connecting).Count();
            var connected = PeerConnectionsActive.Where(c => c.Value?.State == ConnectionState.Connected).Count();
            var disconnected = PeerConnectionsActive.Where(c => c.Value == null || c.Value.State == ConnectionState.Disconnected).Count();

            var queued = PeerConnectionsQueued.Count();

            Console.WriteLine($"█████████████ Peers: Queued: {queued} Total: {total}, Connecting: {connecting}, Connected: {connected}, Disconnected: {disconnected}");
        }
    }
}