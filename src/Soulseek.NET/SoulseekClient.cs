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

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class with the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The client options.</param>
        public SoulseekClient(SoulseekClientOptions options = null)
            : this("vps.slsknet.org", 2271, options)
        {

        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class with the specified <paramref name="address"/>
        ///     and <paramref name="port"/>.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="options">The client options.</param>
        public SoulseekClient(string address, int port, SoulseekClientOptions options)
        {
            Address = address;
            Port = port;
            Options = options ?? new SoulseekClientOptions();

            Connection = new Connection(ConnectionType.Server, Address, Port, Options.ConnectionTimeout, Options.ReadTimeout, Options.BufferSize);
            Connection.StateChanged += OnServerConnectionStateChanged;
            Connection.DataReceived += OnServerConnectionDataReceived;
        }

        /// <summary>
        ///     Occurs when the underlying TCP connection to the server changes state.
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        ///     Occurs when raw data is received by the underlying TCP connection.
        /// </summary>
        public event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        ///     Occurs when a new message is received.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     Occurs when a search is completed.
        /// </summary>
        public event EventHandler<SearchCompletedEventArgs> SearchEnded;

        /// <summary>
        ///     Occurs when a new search result is received.
        /// </summary>
        public event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        /// <summary>
        ///     Gets or sets the address of the server to which to connect.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        ///     Gets the client options.
        /// </summary>
        public SoulseekClientOptions Options { get; private set; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        public ConnectionState ConnectionState => Connection.State;

        /// <summary>
        ///     Gets information about peer connections.
        /// </summary>
        public PeerInfo Peers => GetPeerInfo();

        /// <summary>
        ///     Gets or sets the port to which to connect.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     Gets information about the connected server.
        /// </summary>
        public ServerInfo Server { get; private set; } = new ServerInfo();

        private Connection Connection { get; set; }
        private bool Disposed { get; set; } = false;
        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();
        private ConcurrentDictionary<ConnectToPeerResponse, Connection> PeerConnectionsActive { get; set; } = new ConcurrentDictionary<ConnectToPeerResponse, Connection>();
        private ConcurrentQueue<KeyValuePair<ConnectToPeerResponse, Connection>> PeerConnectionsQueued { get; set; } = new ConcurrentQueue<KeyValuePair<ConnectToPeerResponse, Connection>>();
        private Random Random { get; set; } = new Random();

        private Search ActiveSearch { get; set; }

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
        ///     Disconnects the client from the server with an optionally supplied <paramref name="message"/>.
        /// </summary>
        /// <param name="message">An optional disconnect message.</param>
        public void Disconnect(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                message = "Client disconnected.";
            }

            Connection.Disconnect(message);

            ClearPeerConnectionsQueued();
            ClearPeerConnectionsActive(message);
            ActiveSearch.Dispose();
            ActiveSearch = default(Search);
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

            Server.Rooms = (IEnumerable<Room>)roomList.Result;
            Server.ParentMinSpeed = ((int)parentMinSpeed.Result);
            Server.ParentSpeedRatio = ((int)parentSpeedRatio.Result);
            Server.WishlistInterval = ((int)wishlistInterval.Result);
            Server.PrivilegedUsers = (IEnumerable<string>)privilegedUsers.Result;

            return (LoginResponse)login.Result;
        }

        /// <summary>
        ///     Asynchronously performs a search for the specified <paramref name="searchText"/> using the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="options">The options for the search.</param>
        /// <returns>The completed search.</returns>
        public async Task<Search> SearchAsync(string searchText, SearchOptions options = null)
        {
            var search = await StartSearchAsync(searchText, options);
            var result = await MessageWaiter.Wait(MessageCode.ServerFileSearch, search.Ticket).Task;

            return (Search)result;
        }

        /// <summary>
        ///     Asynchronously starts a search for the specified <paramref name="searchText"/> using the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="options">The options for the search.</param>
        /// <returns>The started search.</returns>
        public async Task<Search> StartSearchAsync(string searchText, SearchOptions options = null)
        {
            if (ActiveSearch != default(Search))
            {
                throw new SearchException($"A search is already in progress.");
            }

            options = options ?? new SearchOptions();

            ActiveSearch = new Search(searchText, options, Connection);
            ActiveSearch.SearchEnded += OnSearchEnded;
            ActiveSearch.SearchResponseReceived += OnSearchResponseReceived;

            await ActiveSearch.StartAsync();

            return ActiveSearch;
        }

        /// <summary>
        ///     Asynchronously stops the specified <paramref name="search"/>.
        /// </summary>
        /// <param name="search">The search to stop.</param>
        /// <returns>The completed search.</returns>
        public async Task<Search> StopSearchAsync(Search search)
        {
            if (ActiveSearch.Ticket != search.Ticket)
            {
                throw new SearchException($"The requested search is not presently active.");
            }
            if (ActiveSearch.State != SearchState.InProgress)
            {
                throw new SearchException($"The requested search has already completed.");
            }

            var wait = MessageWaiter.Wait(MessageCode.ServerFileSearch, ActiveSearch.Ticket);
            ActiveSearch.Stop();
            var result = await wait.Task;

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
                }

                Disposed = true;
            }
        }

        private void ClearPeerConnectionsActive(string disconnectMessage)
        {
            while (!PeerConnectionsActive.IsEmpty)
            {
                var key = PeerConnectionsActive.Keys.First();

                if (PeerConnectionsActive.TryRemove(key, out var connection))
                {
                    try
                    {
                        connection?.Disconnect(disconnectMessage);
                        connection?.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private void ClearPeerConnectionsQueued()
        {
            while (!PeerConnectionsQueued.IsEmpty)
            {
                if (PeerConnectionsQueued.TryDequeue(out var queuedConnection))
                {
                    try
                    {
                        queuedConnection.Value?.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private PeerInfo GetPeerInfo()
        {
            return new PeerInfo()
            {
                Active = PeerConnectionsActive.Count(),
                Queued = PeerConnectionsQueued.Count(),
                Connecting = PeerConnectionsActive.Where(c => c.Value?.State == ConnectionState.Connecting).Count(),
                Connected = PeerConnectionsActive.Where(c => c.Value?.State == ConnectionState.Connected).Count(),
                Disconnecting = PeerConnectionsActive.Where(c => c.Value?.State == ConnectionState.Disconnecting).Count(),
                Disconnected = PeerConnectionsActive.Where(c => c.Value == null || c.Value.State == ConnectionState.Disconnected).Count(),
            };
        }

        private void HandlePeerSearchResponse(SearchResponse response, NetworkEventArgs e)
        {
            if (response != null)
            {
                ActiveSearch?.AddResponse(response, e);
            }
        }

        private async Task HandlePrivateMessage(PrivateMessage message, NetworkEventArgs e)
        {
            Console.WriteLine($"[{message.Timestamp}][{message.Username}]: {message.Message}");
            await Connection.SendAsync(new AcknowledgePrivateMessageRequest(message.Id).ToByteArray());
        }

        private async Task HandleServerConnectToPeer(ConnectToPeerResponse response, NetworkEventArgs e)
        {
            var connection = new Connection(ConnectionType.Peer, response.IPAddress.ToString(), response.Port, Options.ConnectionTimeout, Options.ReadTimeout, Options.BufferSize)
            {
                Context = response
            };

            connection.DataReceived += OnPeerConnectionDataReceived;
            connection.StateChanged += OnPeerConnectionStateChanged;

            if (PeerConnectionsActive.Count() < Options.ConcurrentPeerConnections)
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

        private async void OnServerConnectionDataReceived(object sender, DataReceivedEventArgs e)
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
                    // todo: create SearchResponseSlim and pass that instead
                    // do this to avoid parsing a bunch of files that may not meet search criteria (number of slots, queue, number of files)
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

        private void OnSearchEnded(object sender, SearchCompletedEventArgs e)
        {
            ClearPeerConnectionsQueued();
            ClearPeerConnectionsActive("Search completed.");
            ActiveSearch = default(Search);

            MessageWaiter.Complete(MessageCode.ServerFileSearch, e.Search.Ticket, e.Search);
            Task.Run(() => SearchEnded?.Invoke(this, e));
        }

        private void OnSearchResponseReceived(object sender, SearchResponseReceivedEventArgs e)
        {
            Task.Run(() => SearchResponseReceived?.Invoke(this, e));
        }

        private async void OnServerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            await Task.Run(() => ConnectionStateChanged?.Invoke(this, e));
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
    }
}