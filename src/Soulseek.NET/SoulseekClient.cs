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
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : ISoulseekClient
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
            IWaiter messageWaiter = null,
            ITokenFactory tokenFactory = null)
        {
            Address = address;
            Port = port;

            Options = options ?? new SoulseekClientOptions();

            ServerConnection = serverConnection ?? GetServerMessageConnection(Address, Port, Options.ConnectionOptions);
            PeerConnectionManager = peerConnectionManager ?? new ConnectionManager<IMessageConnection>(Options.ConcurrentPeerConnections);
            MessageWaiter = messageWaiter ?? new Waiter(Options.MessageTimeout);
            TokenFactory = tokenFactory ?? new TokenFactory();
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
        public SoulseekClientStates State { get; private set; } = SoulseekClientStates.Disconnected;

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
        private ITokenFactory TokenFactory { get; set; }

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
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
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
            if (State.HasFlag(SoulseekClientStates.Connected))
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

            ChangeState(SoulseekClientStates.Disconnected);
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Asynchronously downloads the specified <paramref name="filename"/> from the specified <paramref name="username"/> and with the optionally specified <paramref name="token"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <remarks>
        ///     If no <paramref name="token"/> is specified, one will be randomly generated internally.
        /// </remarks>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="filename">The file to download.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The operation context, including a byte array containing the file contents.</returns>
        public Task<byte[]> DownloadAsync(string username, string filename, int? token = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"The filename must not be a null or empty string, or one consisting only of whitespace.", nameof(filename));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to browse.");
            }

            bool tokenExists(int t) => QueuedDownloads.ContainsKey(t) || ActiveDownloads.Any(d => d.Value.Token == t);
            int tokenInternal;

            if (token != null)
            {
                if (tokenExists((int)token))
                {
                    throw new ArgumentException($"An active or queued download with token {token} is already in progress.", nameof(token));
                }

                tokenInternal = (int)token;
            }
            else
            {
                if (!TokenFactory.TryGetToken(tokenExists, out var generatedToken))
                {
                    throw new DownloadException($"Unable to generate a unique token for the download.");
                }

                tokenInternal = (int)generatedToken;
            }

            return DownloadInternalAsync(username, filename, tokenInternal, cancellationToken, null);
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
            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The client must be connected to log in.");
            }

            if (State.HasFlag(SoulseekClientStates.LoggedIn))
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
            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to search (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
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

                await connection.WriteMessageAsync(new PeerBrowseRequest().ToMessage()).ConfigureAwait(false);

                var response = await browseWait.ConfigureAwait(false);
                return response;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to browse user {Username}: {ex.Message}", ex);
            }
        }

        private void ChangeState(SoulseekClientStates state, string message = null)
        {
            State = state;
            Task.Run(() => StateChanged?.Invoke(this, new SoulseekClientStateChangedEventArgs(state, message)));
        }

        private async Task<byte[]> DownloadInternalAsync(string username, string filename, int token, CancellationToken? cancellationToken = null, IMessageConnection connection = null)
        {
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
                var incomingResponseWait = MessageWaiter.WaitIndefinitely<PeerTransferResponse>(new WaitKey(MessageCode.PeerTransferResponse, download.Username, download.Token), cancellationToken);
                var incomingRequestWait = MessageWaiter.WaitIndefinitely<PeerTransferRequest>(new WaitKey(MessageCode.PeerTransferRequest, download.Username, download.Filename), cancellationToken);

                // request the file and await the response
                await connection.WriteMessageAsync(new PeerTransferRequest(TransferDirection.Download, token, filename).ToMessage()).ConfigureAwait(false);

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
                    download.State = DownloadStates.Queued;
                    QueuedDownloads.TryAdd(download.Token, download);

                    Task.Run(() => DownloadStateChanged?.Invoke(this, new DownloadStateChangedEventArgs(DownloadStates.None, download))).Forget();

                    // wait for the peer to respond that they are ready to start the transfer
                    var incomingRequest = await incomingRequestWait.ConfigureAwait(false);

                    download.Size = incomingRequest.FileSize;
                    download.RemoteToken = incomingRequest.Token;

                    QueuedDownloads.TryRemove(download.Token, out var _);

                    download.State = DownloadStates.InProgress;
                    ActiveDownloads.TryAdd(download.RemoteToken, download);

                    Task.Run(() => DownloadStateChanged?.Invoke(this, new DownloadStateChangedEventArgs(DownloadStates.Queued, download))).Forget();

                    await connection.WriteMessageAsync(new PeerTransferResponse(download.RemoteToken, true, download.Size, string.Empty).ToMessage()).ConfigureAwait(false);
                }

                try
                {
                    download.Data = await downloadWait.ConfigureAwait(false); // completed within ConnectToPeerResponse handling
                    download.State = DownloadStates.Completed;
                }
                catch (OperationCanceledException)
                {
                    download.State = DownloadStates.Completed | DownloadStates.Cancelled;
                    download.Connection.Disconnect("Transfer cancelled.");
                    download.Connection.Dispose();
                }

                // todo: handle download failure
                Task.Run(() => DownloadStateChanged?.Invoke(this, new DownloadStateChangedEventArgs(DownloadStates.InProgress, download))).Forget();

                return download.Data;
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Failed to download file {filename} from user {username}: {ex.Message}", ex);
            }
        }

        private async Task<ConnectionKey> GetPeerConnectionKeyAsync(string username)
        {
            var addressWait = MessageWaiter.Wait<GetPeerAddressResponse>(new WaitKey(MessageCode.ServerGetPeerAddress, username));

            var request = new GetPeerAddressRequest(username);
            await ServerConnection.WriteMessageAsync(request.ToMessage()).ConfigureAwait(false);

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
                ChangeState(SoulseekClientStates.Connected);
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
                await conn.WriteAsync(request.ToByteArray()).ConfigureAwait(false);
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
                    await connection.WriteAsync(new PeerInitRequest(Username, "P", token).ToMessage().ToByteArray()).ConfigureAwait(false);
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
                        if (!download.State.HasFlag(DownloadStates.Completed))
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
                    download.State = DownloadStates.Completed;
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
                    var searchResponse = SearchResponseSlim.Parse(message);
                    if (ActiveSearches.TryGetValue(searchResponse.Token, out var search))
                    {
                        search.AddResponse(searchResponse);
                    }

                    break;

                case MessageCode.PeerBrowseResponse:
                    var browseWaitKey = new WaitKey(MessageCode.PeerBrowseResponse, connection.Key.Username);
                    try
                    {
                        MessageWaiter.Complete(browseWaitKey, BrowseResponse.Parse(message));
                    }
                    catch (Exception ex)
                    {
                        MessageWaiter.Throw(browseWaitKey, new MessageReadException("The peer returned an invalid browse response.", ex));
                    }

                    break;

                case MessageCode.PeerTransferResponse:
                    var transferResponse = PeerTransferResponse.Parse(message);
                    MessageWaiter.Complete(new WaitKey(MessageCode.PeerTransferResponse, connection.Username, transferResponse.Token), transferResponse);
                    break;

                case MessageCode.PeerTransferRequest:
                    var transferRequest = PeerTransferRequest.Parse(message);
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

                case MessageCode.ServerPrivateMessage:
                    var pm = PrivateMessage.Parse(message);
                    Console.WriteLine($"[{pm.Timestamp}][{pm.Username}]: {pm.Message}");
                    await ServerConnection.WriteMessageAsync(new AcknowledgePrivateMessageRequest(pm.Id).ToMessage()).ConfigureAwait(false);
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

            await ServerConnection.WriteMessageAsync(new LoginRequest(username, password).ToMessage()).ConfigureAwait(false);

            var response = await loginWait.ConfigureAwait(false);

            if (response.Succeeded)
            {
                Username = username;
                ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
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

                await ServerConnection.WriteMessageAsync(new SearchRequest(search.SearchText, search.Token).ToMessage()).ConfigureAwait(false);

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
                    search.Complete(SearchStates.Completed | SearchStates.Cancelled);
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