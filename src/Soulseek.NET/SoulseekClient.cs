// <copyright file="SoulseekClient.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.NET
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : IDisposable, ISoulseekClient
    {
        #region Public Constructors

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

            MessageWaiter = new MessageWaiter(Options.MessageTimeout);
        }

        #endregion Public Constructors

        #region Public Events

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
        ///     Occurs when a new search result is received.
        /// </summary>
        public event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        #endregion Public Events

        #region Public Properties

        public string Username { get; private set; }
        public bool LoggedIn { get; private set; } = false;

        /// <summary>
        ///     Gets or sets the address of the server to which to connect.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        public ConnectionState ConnectionState => Connection.State;

        /// <summary>
        ///     Gets the client options.
        /// </summary>
        public SoulseekClientOptions Options { get; private set; }

        /// <summary>
        ///     Gets or sets the port to which to connect.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     Gets information about the connected server.
        /// </summary>
        public ServerInfo Server { get; private set; } = new ServerInfo();

        #endregion Public Properties

        #region Private Properties

        private Search ActiveSearch { get; set; }
        private Connection Connection { get; set; }
        private bool Disposed { get; set; } = false;
        private MessageWaiter MessageWaiter { get; set; }
        private Random Random { get; set; } = new Random();

        #endregion Private Properties

        #region Public Methods

        public async Task<SharesResponse> BrowseAsync(string username, BrowseOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (ConnectionState != ConnectionState.Connected)
            {
                throw new ConnectionStateException($"The server connection must be Connected to browse (currently: {ConnectionState})");
            }

            if (!LoggedIn)
            {
                throw new SearchException($"A user must be logged in to browse.");
            }

            options = options ?? new BrowseOptions();

            var address = await GetPeerAddressAsync(username);
            var browse = new Browse(username, address.IPAddress, address.Port, options, cancellationToken);

            return await browse.BrowseAsync();
        }

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ConnectAsync()
        {
            // todo: fail if already connected
            await Connection.ConnectAsync();
        }

        /// <summary>
        ///     Disconnects the client from the server with an optionally supplied <paramref name="message"/>.
        /// </summary>
        public void Disconnect()
        {
            var message = "Client disconnected.";

            Connection.Disconnect(message);

            ActiveSearch.Dispose();

            ActiveSearch = default(Search);
            Username = null;
            LoggedIn = false;
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        //public async Task<bool> Download(string username, string filename)
        //{
        //    // todo: fail if not logged in

        //    var address = await GetPeerAddressAsync(username);

        //    Console.WriteLine($"[DOWNLOAD]: {username} {address.IPAddress}:{address.Port}");

        //    var peerConnection = new Connection(ConnectionType.Peer, address.IPAddress, address.Port);
        //    peerConnection.DataReceived += OnPeerConnectionDataReceived;
        //    peerConnection.StateChanged += OnPeerConnectionStateChanged;

        //    try
        //    {
        //        await peerConnection.ConnectAsync();

        //        var token = new Random().Next();
        //        await peerConnection.SendAsync(new PeerInitRequest(Username, "P", token).ToByteArray(), suppressCodeNormalization: true);
        //        await peerConnection.SendAsync(new PeerTransferRequest(TransferDirection.Download, token, @"@@djpnk\Bootlegs\30 Songs for a Revolution\album.nfo").ToMessage().ToByteArray());
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Failed to download {filename} from {username}: {ex.Message}");
        //    }

        //    return true;
        //}

        /// <summary>
        ///     Asynchronously logs in to the server with the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <returns>The server response.</returns>
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            // todo: fail if already logged in

            Username = username;
            var request = new LoginRequest(username, password);

            var login = MessageWaiter.Wait<LoginResponse>(MessageCode.ServerLogin);
            var roomList = MessageWaiter.Wait<IEnumerable<Room>>(MessageCode.ServerRoomList);
            var parentMinSpeed = MessageWaiter.Wait<int>(MessageCode.ServerParentMinSpeed);
            var parentSpeedRatio = MessageWaiter.Wait<int>(MessageCode.ServerParentSpeedRatio);
            var wishlistInterval = MessageWaiter.Wait<int>(MessageCode.ServerWishlistInterval);
            var privilegedUsers = MessageWaiter.Wait<IEnumerable<string>>(MessageCode.ServerPrivilegedUsers);

            await Connection.SendAsync(request.ToMessage().ToByteArray());

            Task.WaitAll(login, roomList, parentMinSpeed, parentSpeedRatio, wishlistInterval, privilegedUsers);

            Server.Rooms = roomList.Result;
            Server.ParentMinSpeed = parentMinSpeed.Result;
            Server.ParentSpeedRatio = parentSpeedRatio.Result;
            Server.WishlistInterval = wishlistInterval.Result;
            Server.PrivilegedUsers = privilegedUsers.Result;

            LoggedIn = login.Result.Succeeded;

            return login.Result;
        }

        /// <summary>
        ///     Asynchronously performs a search for the specified <paramref name="searchText"/> using the specified <paramref name="options"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="options">The options for the search.</param>
        /// <param name="cancellationToken">The optional cancellation token for the task.</param>
        /// <returns>The completed search.</returns>
        public async Task<Search> SearchAsync(string searchText, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (ConnectionState != ConnectionState.Connected)
            {
                throw new ConnectionStateException($"The server connection must be Connected to perform a search (currently: {ConnectionState})");
            }

            if (!LoggedIn)
            {
                throw new SearchException($"A user must be logged in to perform a search.");
            }

            if (ActiveSearch != default(Search))
            {
                throw new SearchException($"A search is already in progress.");
            }

            options = options ?? new SearchOptions(soulseekClientOptions: Options);

            ActiveSearch = new Search(searchText, options, Connection);
            ActiveSearch.SearchResponseReceived += OnSearchResponseReceived;

            return await ActiveSearch.SearchAsync(cancellationToken);
        }

        #endregion Public Methods

        #region Protected Methods

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        /// <param name="disposing">A value indicating whether disposal is in progress.</param>
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

        #endregion Protected Methods

        #region Private Methods

        private async Task<GetPeerAddressResponse> GetPeerAddressAsync(string username)
        {
            var request = new GetPeerAddressRequest(username);
            await Connection.SendAsync(request.ToMessage().ToByteArray());

            return await MessageWaiter.Wait<GetPeerAddressResponse>(MessageCode.ServerGetPeerAddress, username);
        }

        private async Task HandlePrivateMessage(PrivateMessage message, NetworkEventArgs e)
        {
            Console.WriteLine($"[{message.Timestamp}][{message.Username}]: {message.Message}");
            await Connection.SendAsync(new AcknowledgePrivateMessageRequest(message.Id).ToByteArray());
        }

        private async Task HandleServerConnectToPeer(ConnectToPeerResponse response, NetworkEventArgs e)
        {
            if (ActiveSearch != default(Search))
            {
                await ActiveSearch.AddPeerConnection(response, e);
            }
        }

        private void OnSearchResponseReceived(object sender, SearchResponseReceivedEventArgs e)
        {
            Task.Run(() => SearchResponseReceived?.Invoke(this, e));
        }

        private async void OnServerConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Task.Run(() => DataReceived?.Invoke(this, e)).Forget();

            var message = new Message(e.Data);
            var messageEventArgs = new MessageReceivedEventArgs(e) { Message = message };

            Task.Run(() => MessageReceived?.Invoke(this, messageEventArgs)).Forget();

            //Console.WriteLine($"[MESSAGE]: {message.Code}");

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

                case MessageCode.ServerGetPeerAddress:
                    var response = GetPeerAddressResponse.Parse(message);
                    MessageWaiter.Complete(message.Code, response.Username, response);
                    break;

                default:
                    Console.WriteLine($"Unknown message: [{e.IPAddress}] {message.Code}: {message.Payload.Length} bytes");
                    break;
            }
        }

        private async void OnServerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Disconnected)
            {
                Disconnect();
            }

            await Task.Run(() => ConnectionStateChanged?.Invoke(this, e));
        }

        #endregion Private Methods
    }
}