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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Exceptions;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : IDisposable, ISoulseekClient
    {
        #region Private Fields

        private const string DefaultAddress = "vps.slsknet.org";
        private const int DefaultPort = 2271;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class with the specified <paramref name="address"/>
        ///     and <paramref name="port"/>.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="options">The client <see cref="SoulseekClientOptions"/>.</param>
        public SoulseekClient(string address = DefaultAddress, int port = DefaultPort, SoulseekClientOptions options = null)
            : this(address, port, options, null, null, null)
        {
        }

        #endregion Public Constructors

        #region Internal Constructors

        internal SoulseekClient(
            string address,
            int port,
            SoulseekClientOptions options = null,
            IMessageConnection serverConnection = null,
            IConnectionManager<IMessageConnection> peerConnectionManager = null,
            IWaiter messageWaiter = null)
        {
            Address = address;
            Port = port;

            Options = options ?? new SoulseekClientOptions();

            ServerConnection = serverConnection ?? GetServerMessageConnection(Address, Port, Options.ConnectionOptions);
            PeerConnectionManager = peerConnectionManager ?? new ConnectionManager<IMessageConnection>(Options.ConcurrentPeerConnections);
            MessageWaiter = messageWaiter ?? new Waiter(Options.MessageTimeout);
        }

        #endregion Internal Constructors

        #region Private Destructors

        /// <summary>
        ///     Finalizes an instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        ~SoulseekClient()
        {
            Dispose(false);
        }

        #endregion Private Destructors

        #region Public Events

        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        public event EventHandler<DownloadStateChangedEventArgs> DownloadStateChanged;

        /// <summary>
        ///     Occurs when a new search result is received.
        /// </summary>
        public event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        public event EventHandler<SearchStateChangedEventArgs> SearchStateChanged;

        /// <summary>
        ///     Occurs when the client changes state.
        /// </summary>
        public event EventHandler<SoulseekClientStateChangedEventArgs> StateChanged;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        ///     Gets or sets the address of the server to which to connect.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        ///     Gets the client options.
        /// </summary>
        public SoulseekClientOptions Options { get; private set; }

        /// <summary>
        ///     Gets or sets the port to which to connect.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        public SoulseekClientState State { get; private set; }

        /// <summary>
        ///     Gets the name of the currently signed in user.
        /// </summary>
        public string Username { get; private set; }

        #endregion Public Properties

        #region Private Properties

        private ConcurrentDictionary<int, Download> ActiveDownloads { get; set; } = new ConcurrentDictionary<int, Download>();
        private ConcurrentDictionary<int, Search> ActiveSearches { get; set; } = new ConcurrentDictionary<int, Search>();
        private bool Disposed { get; set; } = false;
        private IWaiter MessageWaiter { get; set; }
        private IConnectionManager<IMessageConnection> PeerConnectionManager { get; set; }
        private ConcurrentDictionary<int, Download> QueuedDownloads { get; set; } = new ConcurrentDictionary<int, Download>();
        private Random Random { get; set; } = new Random();
        private IMessageConnection ServerConnection { get; set; }

        #endregion Private Properties

        #region Public Methods

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally
        ///     specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to browse.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The operation response.</returns>
        public Task<BrowseResponse> BrowseAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientState.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientState.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to browse.");
            }

            return BrowseInternalAsync(username, cancellationToken, null);
        }

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is already connected.</exception>
        /// <exception cref="ConnectionException">
        ///     Thrown when the client is already connected, or is transitioning between states.
        /// </exception>
        public async Task ConnectAsync()
        {
            if (State.HasFlag(SoulseekClientState.Connected))
            {
                throw new InvalidOperationException($"Failed to connect; the client is already connected.");
            }

            try
            {
                await ServerConnection.ConnectAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new ConnectionException($"Failed to connect: {ex.Message}.", ex);
            }
        }

        /// <summary>
        ///     Disconnects the client from the server.
        /// </summary>
        /// <param name="message">An optional message describing the reason the client is being disconnected.</param>
        public void Disconnect(string message = null)
        {
            ServerConnection?.Disconnect(message ?? "Client disconnected.");

            PeerConnectionManager?.RemoveAll();

            ActiveSearches?.RemoveAndDisposeAll();

            QueuedDownloads?.RemoveAll();
            ActiveDownloads?.RemoveAll();

            MessageWaiter?.CancelAll();

            Username = null;

            ChangeState(SoulseekClientState.Disconnected);
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Task<byte[]> DownloadAsync(string username, string filename, int token, CancellationToken? cancellationToken = null)
        {
            return DownloadInternalAsync(username, filename, token, cancellationToken, null);
        }

        /// <summary>
        ///     Asynchronously logs in to the server with the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <returns>A Task representing the operation.</returns>
        /// <exception cref="LoginException">Thrown when the login fails.</exception>
        public Task LoginAsync(string username, string password)
        {
            if (!State.HasFlag(SoulseekClientState.Connected))
            {
                throw new InvalidOperationException($"The client must be connected to log in.");
            }

            if (State.HasFlag(SoulseekClientState.LoggedIn))
            {
                throw new InvalidOperationException($"Already logged in as {Username}.  Disconnect before logging in again.");
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("Username may not be null or an empty string.", nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password may not be null or an empty string.", nameof(password));
            }

            return LoginInternalAsync(username, password);
        }

        /// <summary>
        ///     Asynchronously searches for the specified <paramref name="searchText"/> and unique <paramref name="token"/> and
        ///     with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The operation <see cref="SearchOptions"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <param name="waitForCompletion">A value indicating whether the search should wait completion before returning.</param>
        /// <returns>The operation context, including the search results.</returns>
        /// <exception cref="ConnectionException">
        ///     Thrown when the client is not connected to the server, or no user is logged in.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the specified <paramref name="searchText"/> is null, empty, or consists of only whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when a search with the specified <paramref name="token"/> is already in progress.
        /// </exception>
        /// <exception cref="SearchException">Thrown when an unhandled Exception is encountered during the operation.</exception>
        public Task<IEnumerable<SearchResponse>> SearchAsync(string searchText, int token, SearchOptions options = null, CancellationToken? cancellationToken = null, bool waitForCompletion = true)
        {
            if (!State.HasFlag(SoulseekClientState.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to search (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientState.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to search.");
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException($"Search text must not be a null or empty string, or one consisting only of whitespace.", nameof(searchText));
            }

            if (ActiveSearches.ContainsKey(token))
            {
                throw new ArgumentException($"An active search with token {token} is already in progress.", nameof(token));
            }

            return SearchInternalAsync(searchText, token, options, cancellationToken, waitForCompletion);
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
                Disconnect("Client is being disposed.");

                if (disposing)
                {
                    PeerConnectionManager?.Dispose();
                    MessageWaiter?.Dispose();
                    ServerConnection?.Dispose();
                }

                Disposed = true;
            }
        }

        #endregion Protected Methods

        #region Private Methods

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally
        ///     specified <paramref name="cancellationToken"/> and <paramref name="connection"/>.
        /// </summary>
        /// <param name="username">The user to browse.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <param name="connection">The peer connection over which to send the browse request.</param>
        /// <returns>The operation response.</returns>
        private async Task<BrowseResponse> BrowseInternalAsync(string username, CancellationToken? cancellationToken = null, IMessageConnection connection = null)
        {
            try
            {
                var browseWait = MessageWaiter.WaitIndefinitely<BrowseResponse>(new WaitKey(MessageCode.PeerBrowseResponse, username), cancellationToken);

                connection = connection ?? await GetUnsolicitedPeerConnectionAsync(username, Options.PeerConnectionOptions).ConfigureAwait(false);
                connection.Disconnected += (sender, message) =>
                {
                    MessageWaiter.Throw(new WaitKey(MessageCode.PeerBrowseResponse, ((IMessageConnection)sender).Key.Username), new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                };

                await connection.SendMessageAsync(new PeerBrowseRequest().ToMessage()).ConfigureAwait(false);

                var response = await browseWait.ConfigureAwait(false);
                return response;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to browse user {Username}: {ex.Message}", ex);
            }
        }

        private void ChangeState(SoulseekClientState state, string message = null)
        {
            State = state;
            Task.Run(() => StateChanged?.Invoke(this, new SoulseekClientStateChangedEventArgs(state, message)));
        }

        private async Task<byte[]> DownloadInternalAsync(string username, string filename, int token, CancellationToken? cancellationToken = null, IMessageConnection connection = null)
        {
            // todo: check arguments
            // todo: implement overall exception handling
            // todo: catch OperationCancelledException
            try
            {
                var download = new Download(username, filename, token);
                var downloadWait = MessageWaiter.WaitIndefinitely<byte[]>(new WaitKey(MessageCode.PeerDownloadResponse, download.WaitKey), cancellationToken);

                // establish a message connection to the peer
                connection = connection ?? await GetUnsolicitedPeerConnectionAsync(username, Options.PeerConnectionOptions).ConfigureAwait(false);
                connection.Disconnected += (sender, message) =>
                {
                    MessageWaiter.Throw(new WaitKey(MessageCode.PeerDownloadResponse, download.WaitKey), new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                };

                // prepare two waits; one for the transfer response and another for the eventual transfer request sent when the
                // peer is ready to send the file.
                var incomingResponseWait = MessageWaiter.WaitIndefinitely<PeerTransferResponseIncoming>(new WaitKey(MessageCode.PeerTransferResponse, download.Username, download.Token), cancellationToken);
                var incomingRequestWait = MessageWaiter.WaitIndefinitely<PeerTransferRequestIncoming>(new WaitKey(MessageCode.PeerTransferRequest, download.Username, download.Filename), cancellationToken);

                // request the file and await the response
                await connection.SendMessageAsync(new PeerTransferRequestOutgoing(TransferDirection.Download, token, filename).ToMessage()).ConfigureAwait(false);

                var incomingResponse = await incomingResponseWait.ConfigureAwait(false);

                if (incomingResponse.Allowed)
                {
                    // in testing, peers have, without exception, returned Allowed = false, Message = Queued for this request,
                    // regardless of number of available slots and/or queue depth. this condition is likely only used when
                    // uploading to a peer, which is not supported.
                    throw new DownloadException($"A condition believed to be unreachable (PeerTransferResponseIncoming.Allowed = true) was reached.  Please report this in a GitHub issue and provide context.");
                }
                else
                {
                    // todo: get place in line
                    download.State = DownloadState.Queued;
                    QueuedDownloads.TryAdd(download.Token, download);

                    Task.Run(() => DownloadStateChanged?.Invoke(this, new DownloadStateChangedEventArgs(download))).Forget();

                    // wait for the peer to respond that they are ready to start the transfer
                    var incomingRequest = await incomingRequestWait.ConfigureAwait(false);

                    download.Size = incomingRequest.FileSize;
                    download.RemoteToken = incomingRequest.Token;

                    QueuedDownloads.TryRemove(download.Token, out var _);

                    download.State = DownloadState.InProgress;
                    ActiveDownloads.TryAdd(download.RemoteToken, download);

                    Task.Run(() => DownloadStateChanged?.Invoke(this, new DownloadStateChangedEventArgs(download))).Forget();

                    await connection.SendMessageAsync(new PeerTransferResponseOutgoing(download.RemoteToken, true, download.Size, string.Empty).ToMessage()).ConfigureAwait(false);
                }

                try
                {
                    download.Data = await downloadWait.ConfigureAwait(false); // completed within ConnectToPeerResponse handling
                }
                catch (OperationCanceledException)
                {
                    download.State = DownloadState.Cancelled;
                    download.Connection.Disconnect("Transfer cancelled.");
                    download.Connection.Dispose();
                }

                // todo: handle download failure
                Task.Run(() => DownloadStateChanged?.Invoke(this, new DownloadStateChangedEventArgs(download))).Forget();

                return download.Data;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to download file {filename} from user {username}: {ex.Message}", ex);
            }
        }

        private async Task<ConnectionKey> GetPeerConnectionKeyAsync(string username)
        {
            var addressWait = MessageWaiter.Wait<GetPeerAddressResponse>(new WaitKey(MessageCode.ServerGetPeerAddress, username));

            var request = new GetPeerAddressRequest(username);
            await ServerConnection.SendMessageAsync(request.ToMessage()).ConfigureAwait(false);

            var address = await addressWait.ConfigureAwait(false);
            return new ConnectionKey(username, address.IPAddress, address.Port, MessageConnectionType.Peer);
        }

        private IMessageConnection GetServerMessageConnection(string address, int port, ConnectionOptions options)
        {
            var ipAddress = default(IPAddress);

            try
            {
                ipAddress = address.ResolveIPAddress();
            }
            catch (Exception ex)
            {
                throw new SoulseekClientException($"Failed to resolve address '{address}': {ex.Message}", ex);
            }

            var conn = new MessageConnection(MessageConnectionType.Server, ipAddress, port, options);
            conn.Connected += (sender, e) =>
            {
                ChangeState(SoulseekClientState.Connected);
            };

            conn.Disconnected += (sender, e) =>
            {
                Disconnect();
            };

            conn.MessageRead += HandleServerMessage;

            return conn;
        }

        private async Task<IMessageConnection> GetSolicitedPeerConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options)
        {
            var connection = new MessageConnection(MessageConnectionType.Peer, connectToPeerResponse.Username, connectToPeerResponse.IPAddress, connectToPeerResponse.Port, options)
            {
                Context = connectToPeerResponse,
            };

            connection.Connected += async (sender, e) =>
            {
                var conn = (IMessageConnection)sender;
                var context = (ConnectToPeerResponse)conn.Context;
                var request = new PierceFirewallRequest(context.Token).ToMessage();
                await conn.SendMessageAsync(request, suppressCodeNormalization: true).ConfigureAwait(false);
            };

            connection.Disconnected += async (sender, e) =>
            {
                await PeerConnectionManager.RemoveAsync((IMessageConnection)sender).ConfigureAwait(false);
            };

            connection.MessageRead += HandlePeerMessage;

            await PeerConnectionManager.AddAsync(connection).ConfigureAwait(false);
            return connection;
        }

        private async Task<IConnection> GetTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options)
        {
            return await GetTransferConnectionAsync(connectToPeerResponse.IPAddress, connectToPeerResponse.Port, connectToPeerResponse.Token, options).ConfigureAwait(false);
        }

        private async Task<IConnection> GetTransferConnectionAsync(IPAddress ipAddress, int port, int token, ConnectionOptions options)
        {
            var connection = new Connection(ipAddress, port, options);
            await connection.ConnectAsync().ConfigureAwait(false);

            var request = new PierceFirewallRequest(token);
            await connection.WriteAsync(request.ToMessage().ToByteArray()).ConfigureAwait(false);

            return connection;
        }

        private async Task<IMessageConnection> GetUnsolicitedPeerConnectionAsync(string username, ConnectionOptions options)
        {
            var key = await GetPeerConnectionKeyAsync(username).ConfigureAwait(false);
            var connection = PeerConnectionManager.Get(key);

            if (connection != default(IMessageConnection) && (connection.State == ConnectionState.Disconnecting || connection.State == ConnectionState.Disconnected))
            {
                await PeerConnectionManager.RemoveAsync(connection).ConfigureAwait(false);
                connection = default(IMessageConnection);
            }

            if (connection == default(IMessageConnection))
            {
                connection = new MessageConnection(MessageConnectionType.Peer, key.Username, key.IPAddress, key.Port, options);
                connection.MessageRead += HandlePeerMessage;

                connection.Connected += async (sender, e) =>
                {
                    var token = new Random().Next(1, 2147483647);
                    await connection.SendMessageAsync(new PeerInitRequest(Username, "P", token).ToMessage(), suppressCodeNormalization: true).ConfigureAwait(false);
                };

                connection.Disconnected += async (sender, e) =>
                {
                    await PeerConnectionManager.RemoveAsync((IMessageConnection)sender).ConfigureAwait(false);
                };

                await PeerConnectionManager.AddAsync(connection).ConfigureAwait(false);
            }

            return connection;
        }

        private async Task HandleConnectToPeer(ConnectToPeerResponse response)
        {
            if (response.Type == "F" && !ActiveDownloads.IsEmpty && ActiveDownloads.Select(kvp => kvp.Value).Any(d => d.Username == response.Username))
            {
                var connection = await GetTransferConnectionAsync(response, Options.TransferConnectionOptions).ConfigureAwait(false);
                var tokenBytes = await connection.ReadAsync(4).ConfigureAwait(false);
                var token = BitConverter.ToInt32(tokenBytes, 0);

                if (ActiveDownloads.TryGetValue(token, out var download))
                {
                    connection.Disconnected += (sender, message) =>
                    {
                        if (download.State != DownloadState.Completed)
                        {
                            MessageWaiter.Throw(new WaitKey(MessageCode.PeerDownloadResponse, download.WaitKey), new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                        }
                    };

                    connection.DataRead += (sender, e) =>
                    {
                        var eventArgs = new DownloadProgressEventArgs(download, e.CurrentLength);

                        if (Options.UseSynchronousDownloadProgressEvents)
                        {
                            DownloadProgress?.Invoke(this, eventArgs); // ensure order; impacts performance.
                        }
                        else
                        {
                            Task.Run(() => DownloadProgress?.Invoke(this, eventArgs)).Forget();
                        }
                    };

                    download.Connection = connection;

                    await connection.WriteAsync(new byte[8]).ConfigureAwait(false);

                    var bytes = await connection.ReadAsync(download.Size).ConfigureAwait(false);

                    download.Data = bytes;
                    download.State = DownloadState.Completed;
                    connection.Disconnect($"Transfer complete.");

                    MessageWaiter.Complete(new WaitKey(MessageCode.PeerDownloadResponse, download.WaitKey), bytes);
                }
            }
            else
            {
                await GetSolicitedPeerConnectionAsync(response, Options.PeerConnectionOptions).ConfigureAwait(false);
            }
        }

        private void HandlePeerMessage(object sender, Message message)
        {
            Console.WriteLine($"[PEER MESSAGE]: {message.Code}");

            var connection = (IMessageConnection)sender;

            switch (message.Code)
            {
                case MessageCode.PeerSearchResponse:
                    var searchResponse = SearchResponse.Parse(message);
                    if (ActiveSearches.TryGetValue(searchResponse.Token, out var search))
                    {
                        search.AddResponse(searchResponse);
                    }

                    break;

                case MessageCode.PeerBrowseResponse:
                    MessageWaiter.Complete(new WaitKey(MessageCode.PeerBrowseResponse, connection.Key.Username), BrowseResponse.Parse(message));
                    break;

                case MessageCode.PeerTransferResponse:
                    var transferResponse = PeerTransferResponseIncoming.Parse(message);
                    MessageWaiter.Complete(new WaitKey(MessageCode.PeerTransferResponse, connection.Username, transferResponse.Token), transferResponse);
                    break;

                case MessageCode.PeerTransferRequest:
                    var transferRequest = PeerTransferRequestIncoming.Parse(message);
                    MessageWaiter.Complete(new WaitKey(MessageCode.PeerTransferRequest, connection.Username, transferRequest.Filename), transferRequest);

                    break;

                case MessageCode.PeerQueueFailed:
                    var pqfResponse = PeerQueueFailedResponse.Parse(message);
                    Console.WriteLine($"[PEER QUEUE FAILED]: {pqfResponse.Filename}; {pqfResponse.Message}");

                    break;

                default:
                    Console.WriteLine($"Unknown message: [{connection.IPAddress}] {message.Code}: {message.Payload.Length} bytes");
                    break;
            }
        }

        private async void HandleServerMessage(object sender, Message message)
        {
            Console.WriteLine($"[SERVER MESSAGE]: {message.Code}");

            switch (message.Code)
            {
                case MessageCode.ServerParentMinSpeed:
                case MessageCode.ServerParentSpeedRatio:
                case MessageCode.ServerWishlistInterval:
                    MessageWaiter.Complete(new WaitKey(message.Code), IntegerResponse.Parse(message));
                    break;

                case MessageCode.ServerLogin:
                    MessageWaiter.Complete(new WaitKey(message.Code), LoginResponse.Parse(message));
                    break;

                case MessageCode.ServerRoomList:
                    MessageWaiter.Complete(new WaitKey(message.Code), RoomList.Parse(message));
                    break;

                case MessageCode.ServerPrivilegedUsers:
                    MessageWaiter.Complete(new WaitKey(message.Code), PrivilegedUserList.Parse(message));
                    break;

                case MessageCode.ServerConnectToPeer:
                    await HandleConnectToPeer(ConnectToPeerResponse.Parse(message)).ConfigureAwait(false);
                    break;

                case MessageCode.ServerPrivateMessages:
                    var pm = PrivateMessage.Parse(message);
                    Console.WriteLine($"[{pm.Timestamp}][{pm.Username}]: {pm.Message}");
                    await ServerConnection.SendMessageAsync(new AcknowledgePrivateMessageRequest(pm.Id).ToMessage()).ConfigureAwait(false);
                    break;

                case MessageCode.ServerGetPeerAddress:
                    var response = GetPeerAddressResponse.Parse(message);
                    MessageWaiter.Complete(new WaitKey(message.Code, response.Username), response);
                    break;

                default:
                    Console.WriteLine($"Unknown message: {message.Code}: {message.Payload.Length} bytes");
                    break;
            }
        }

        private async Task LoginInternalAsync(string username, string password)
        {
            var loginWait = MessageWaiter.Wait<LoginResponse>(new WaitKey(MessageCode.ServerLogin));

            await ServerConnection.SendMessageAsync(new LoginRequest(username, password).ToMessage()).ConfigureAwait(false);

            var response = await loginWait.ConfigureAwait(false);

            if (response.Succeeded)
            {
                Username = username;
                ChangeState(SoulseekClientState.Connected | SoulseekClientState.LoggedIn);
            }
            else
            {
                Disconnect(); // upon login failure the server will refuse to allow any more input, eventually disconnecting.
                throw new LoginException($"Failed to log in as {username}: {response.Message}");
            }
        }

        private async Task<IEnumerable<SearchResponse>> SearchInternalAsync(string searchText, int token, SearchOptions options = null, CancellationToken? cancellationToken = null, bool waitForCompletion = true)
        {
            options = options ?? new SearchOptions();

            try
            {
                var searchWait = MessageWaiter.WaitIndefinitely<Search>(new WaitKey(MessageCode.ServerFileSearch, token.ToString()), cancellationToken);

                var search = new Search(searchText, token, options)
                {
                    ResponseHandler = (s, response) =>
                    {
                        var e = new SearchResponseReceivedEventArgs(s, response);
                        Task.Run(() => SearchResponseReceived?.Invoke(this, e)).Forget();
                    },
                    CompleteHandler = (s, state) =>
                    {
                        MessageWaiter.Complete(new WaitKey(MessageCode.ServerFileSearch, token.ToString()), s); // searchWait above
                        ActiveSearches.TryRemove(s.Token, out var _);
                        Task.Run(() => SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(s))).Forget();

                        if (!waitForCompletion)
                        {
                            s.Dispose();
                        }
                    }
                };

                ActiveSearches.TryAdd(search.Token, search);
                Task.Run(() => SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(search))).Forget();

                await ServerConnection.SendMessageAsync(new SearchRequest(search.SearchText, search.Token).ToMessage()).ConfigureAwait(false);

                if (!waitForCompletion)
                {
                    return default(IEnumerable<SearchResponse>);
                }

                try
                {
                    await searchWait.ConfigureAwait(false); // completed in CompleteHandler above
                }
                catch (OperationCanceledException)
                {
                    search.Complete(SearchState.Completed | SearchState.Cancelled);
                }

                var responses = search.Responses;
                search.Dispose();
                return responses;
            }
            catch (Exception ex)
            {
                throw new SearchException($"Failed to search for {searchText} ({token}): {ex.Message}", ex);
            }
        }

        #endregion Private Methods
    }
}