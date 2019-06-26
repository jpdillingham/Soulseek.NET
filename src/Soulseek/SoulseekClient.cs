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

namespace Soulseek
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.ExceptionServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Messaging.Tcp;
    using Soulseek.Tcp;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public class SoulseekClient : ISoulseekClient
    {
        private const string DefaultAddress = "vps.slsknet.org";
        private const int DefaultPort = 2271;

        public SoulseekClient(SoulseekClientResolvers resolvers)
            : this(DefaultAddress, DefaultPort, resolvers, null)
        {

        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        /// <param name="options">The client <see cref="SoulseekClientOptions"/>.</param>
        public SoulseekClient(SoulseekClientOptions options)
            : this(DefaultAddress, DefaultPort, null, options)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="options">The client <see cref="SoulseekClientOptions"/>.</param>
        public SoulseekClient(string address = DefaultAddress, int port = DefaultPort, SoulseekClientResolvers resolvers = null, SoulseekClientOptions options = null)
            : this(address, port, resolvers, options, null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClient"/> class.
        /// </summary>
        /// <param name="address">The address of the server to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="options">The client <see cref="SoulseekClientOptions"/>.</param>
        /// <param name="serverConnection">The IMessageConnection instance to use.</param>
        /// <param name="peerConnectionManager">The IPeerConnectionManager instance to use.</param>
        /// <param name="listener">The IListener instance to use.</param>
        /// <param name="waiter">The IWaiter instance to use.</param>
        /// <param name="tokenFactory">The ITokenFactory instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        internal SoulseekClient(
            string address,
            int port,
            SoulseekClientResolvers resolvers = null,
            SoulseekClientOptions options = null,
            IMessageConnection serverConnection = null,
            IPeerConnectionManager peerConnectionManager = null,
            IListener listener = null,
            IWaiter waiter = null,
            ITokenFactory tokenFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            Address = address;
            Port = port;

            Resolvers = resolvers ?? new SoulseekClientResolvers();
            Options = options ?? new SoulseekClientOptions();

            Waiter = waiter ?? new Waiter(Options.MessageTimeout);
            TokenFactory = tokenFactory ?? new TokenFactory(Options.StartingToken);
            Diagnostic = diagnosticFactory ?? new DiagnosticFactory(this, Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));

            ServerConnection = serverConnection;

            if (ServerConnection == null)
            {
                try
                {
                    IPAddress = Address.ResolveIPAddress();
                }
                catch (SocketException ex)
                {
                    throw new SoulseekClientException($"Failed to resolve address '{address}': {ex.Message}", ex);
                }

                ServerConnection = new MessageConnection(MessageConnectionType.Server, IPAddress, Port, Options.ServerConnectionOptions);
                ServerConnection.Connected += (sender, e) => ChangeState(SoulseekClientStates.Connected);
                ServerConnection.Disconnected += ServerConnection_Disconnected;
                ServerConnection.MessageRead += ServerConnection_MessageRead;
            }

            Listener = listener;

            if (Listener == null && Options.ListenPort.HasValue)
            {
                Listener = new Listener(Options.ListenPort.Value, connectionOptions: Options.IncomingConnectionOptions);
            }

            PeerConnectionManager = peerConnectionManager ?? new PeerConnectionManager(
                soulseekClient: this,
                messageHandler: PeerConnection_MessageRead,
                listener: Listener,
                waiter: Waiter,
                concurrentMessageConnectionLimit: Options.ConcurrentPeerMessageConnectionLimit);

            PeerConnectionManager.DiagnosticGenerated += (sender, e) => DiagnosticGenerated?.Invoke(sender, e);
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Occurs when an active download receives data.
        /// </summary>
        public event EventHandler<DownloadProgressUpdatedEventArgs> DownloadProgressUpdated;

        /// <summary>
        ///     Occurs when a download changes state.
        /// </summary>
        public event EventHandler<DownloadStateChangedEventArgs> DownloadStateChanged;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        public event EventHandler<PrivateMessage> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when a new search result is received.
        /// </summary>
        public event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        /// <summary>
        ///     Occurs when a search changes state.
        /// </summary>
        public event EventHandler<SearchStateChangedEventArgs> SearchStateChanged;

        /// <summary>
        ///     Occurs when the client changes state.
        /// </summary>
        public event EventHandler<SoulseekClientStateChangedEventArgs> StateChanged;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        public event EventHandler<UserStatusChangedEventArgs> UserStatusChanged;

        /// <summary>
        ///     Gets the unresolved server address.
        /// </summary>
        public string Address { get; }

        /// <summary>
        ///     Gets the resolved server address.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the resolved server address.
        /// </summary>
        public SoulseekClientOptions Options { get; }

        public SoulseekClientResolvers Resolvers { get; }

        /// <summary>
        ///     Gets server port.
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        public SoulseekClientStates State { get; private set; } = SoulseekClientStates.Disconnected;

        /// <summary>
        ///     Gets the name of the currently signed in user.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        ///     Gets the server message connection.
        /// </summary>
        internal IMessageConnection ServerConnection { get; }

        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; } = false;
        private ConcurrentDictionary<int, Download> Downloads { get; set; } = new ConcurrentDictionary<int, Download>();
        private IListener Listener { get; }
        private IPeerConnectionManager PeerConnectionManager { get; }
        private ConcurrentDictionary<int, Search> Searches { get; set; } = new ConcurrentDictionary<int, Search>();
        private ITokenFactory TokenFactory { get; }
        private IWaiter Waiter { get; }

        /// <summary>
        ///     Asynchronously sends a private message acknowledgement for the specified <paramref name="privateMessageId"/>.
        /// </summary>
        /// <param name="privateMessageId">The unique id of the private message to acknowledge.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="PrivateMessageException">Thrown when an exception is encountered during the operation.</exception>
        public Task AcknowledgePrivateMessageAsync(int privateMessageId, CancellationToken? cancellationToken = null)
        {
            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to browse.");
            }

            return AcknowledgePrivateMessageInternalAsync(privateMessageId, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously adds the specified <paramref name="username"/> to the server watch list.
        /// </summary>
        /// <param name="username">The username of the user to add.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="AddUserException">Thrown when an exception is encountered during the operation.</exception>
        public Task<AddUserResponse> AddUserAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to add users (currently: {State})");
            }

            return AddUserInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally
        ///     specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to browse.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The operation response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="BrowseException">Thrown when an exception is encountered during the operation.</exception>
        public Task<BrowseResponse> BrowseAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
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

            return BrowseInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is already connected.</exception>
        /// <exception cref="ConnectionException">Thrown when an exception is encountered during the operation.</exception>
        public async Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            if (State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"Failed to connect; the client is already connected.");
            }

            try
            {
                Listener?.Start();

                await ServerConnection.ConnectAsync(cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
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
            ServerConnection.Disconnected -= ServerConnection_Disconnected;
            ServerConnection?.Disconnect(message ?? "Client disconnected.");

            Listener?.Stop();

            PeerConnectionManager?.RemoveAndDisposeAll();

            Searches?.RemoveAndDisposeAll();
            Downloads?.RemoveAll();

            Waiter?.CancelAll();

            Username = null;

            if (State != SoulseekClientStates.Disconnected)
            {
                ChangeState(SoulseekClientStates.Disconnected, message);
            }
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
        ///     Asynchronously downloads the specified <paramref name="filename"/> from the specified <paramref name="username"/>
        ///     using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="filename">The file to download.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="options">The operation <see cref="DownloadOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including a byte array containing the file contents.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DownloadException">Thrown when an exception is encountered during the operation.</exception>
        public Task<byte[]> DownloadAsync(string username, string filename, int? token = null, DownloadOptions options = null, CancellationToken? cancellationToken = null)
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

            token = token ?? TokenFactory.NextToken();

            if (Downloads.ContainsKey((int)token))
            {
                throw new ArgumentException($"An active or queued download with token {token} is already in progress.", nameof(token));
            }

            if (Downloads.Values.Any(d => d.Username == username && d.Filename == filename))
            {
                throw new ArgumentException($"An active of queued download of {filename} from {username} is already in progress.");
            }

            options = options ?? new DownloadOptions();

            return DownloadInternalAsync(username, filename, (int)token, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously gets the current place of the specified <paramref name="filename"/> in the queue of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user whose queue to check.</param>
        /// <param name="filename">The file to check.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The current place of the file in the queue.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DownloadNotFoundException">Thrown when a corresponding download is not active.</exception>
        /// <exception cref="DownloadPlaceInQueueException">Thrown when an exception is encountered during the operation.</exception>
        public Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException($"The filename must not be a null or empty string, or one consisting only of whitespace.", nameof(filename));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be Connected and LoggedIn to check download queue position (currently: {State})");
            }

            if (!Downloads.Any(d => d.Value.Username == username && d.Value.Filename == filename))
            {
                throw new DownloadNotFoundException($"A download of {filename} from user {username} is not active.");
            }

            return GetDownloadPlaceInQueueInternalAsync(username, filename, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Gets the next token for use in client operations.
        /// </summary>
        /// <remarks>
        ///     <para>Tokens are returned sequentially and the token value rolls over to 0 when it has reached <see cref="int.MaxValue"/>.</para>
        ///     <para>This operation is thread safe.</para>
        /// </remarks>
        /// <returns>The next token.</returns>
        /// <threadsafety instance="true"/>
        public int GetNextToken() => TokenFactory.NextToken();

        /// <summary>
        ///     Asynchronously fetches the IP address and port of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the connection information.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the connection information.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the client is not connected to the server, or no user is logged in.
        /// </exception>
        /// <exception cref="UserAddressException">Thrown when an exception is encountered during the operation.</exception>
        public Task<(IPAddress IPAddress, int Port)> GetUserAddressAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user information (currently: {State})");
            }

            return GetUserAddressInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously fetches information about the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the information.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the information response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the client is not connected to the server, or no user is logged in.
        /// </exception>
        /// <exception cref="UserInfoException">Thrown when an exception is encountered during the operation.</exception>
        public Task<UserInfoResponse> GetUserInfoAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user information (currently: {State})");
            }

            return GetUserInfoInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously fetches the status of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user for which to fetch the status.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="UserStatusException">Thrown when an exception is encountered during the operation.</exception>
        public Task<GetStatusResponse> GetUserStatusAsync(string username, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting of only whitespace.", nameof(username));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected) || !State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"The server connection must be connected and logged in to fetch user status (currently: {State})");
            }

            return GetUserStatusInternalAsync(username, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously logs in to the server with the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="LoginException">
        ///     Thrown when the login fails, or when an exception is encountered during the operation.
        /// </exception>
        public Task LoginAsync(string username, string password, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentException("Username may not be null or an empty string.", nameof(username));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Password may not be null or an empty string.", nameof(password));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The client must be connected to log in.");
            }

            if (State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"Already logged in as {Username}.  Disconnect before logging in again.");
            }

            return LoginInternalAsync(username, password, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously searches for the specified <paramref name="searchText"/> using the specified unique
        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The operation <see cref="SearchOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the search results.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the client is not connected to the server, or no user is logged in.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the specified <paramref name="searchText"/> is null, empty, or consists of only whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when a search with the specified <paramref name="token"/> is already in progress.
        /// </exception>
        /// <exception cref="SearchException">Thrown when an unhandled Exception is encountered during the operation.</exception>
        public Task<IReadOnlyCollection<SearchResponse>> SearchAsync(string searchText, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException($"Search text must not be a null or empty string, or one consisting only of whitespace.", nameof(searchText));
            }

            token = token ?? TokenFactory.NextToken();

            if (Searches.ContainsKey((int)token))
            {
                throw new ArgumentException($"An active search with token {token} is already in progress.", nameof(token));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to search (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to search.");
            }

            options = options ?? new SearchOptions();

            return SearchInternalAsync(searchText, (int)token, options, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously sends the specified private <paramref name="message"/> to the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user to which the message is to be sent.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="message"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="PrivateMessageException">Thrown when an exception is encountered during the operation.</exception>
        public Task SendPrivateMessageAsync(string username, string message, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException($"The username must not be a null or empty string, or one consisting only of whitespace.", nameof(username));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException($"The message must not be a null or empty string, or one consisting only of whitespace.", nameof(message));
            }

            if (!State.HasFlag(SoulseekClientStates.Connected))
            {
                throw new InvalidOperationException($"The server connection must be Connected to browse (currently: {State})");
            }

            if (!State.HasFlag(SoulseekClientStates.LoggedIn))
            {
                throw new InvalidOperationException($"A user must be logged in to browse.");
            }

            return SendPrivateMessageInternalAsync(username, message, cancellationToken ?? CancellationToken.None);
        }

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
                    Waiter?.Dispose();
                    ServerConnection?.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task AcknowledgePrivateMessageInternalAsync(int privateMessageId, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteMessageAsync(new AcknowledgePrivateMessageRequest(privateMessageId).ToMessage(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new PrivateMessageException($"Failed to send an acknowledgement for private message id {privateMessageId}: {ex.Message}", ex);
            }
        }

        private async Task<AddUserResponse> AddUserInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var addUserWait = Waiter.Wait<AddUserResponse>(new WaitKey(MessageCode.ServerAddUser, username), cancellationToken: cancellationToken);
                await ServerConnection.WriteMessageAsync(new AddUserRequest(username).ToMessage(), cancellationToken).ConfigureAwait(false);

                var response = await addUserWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                throw new AddUserException($"Failed to retrieve information for user {Username}: {ex.Message}", ex);
            }
        }

        private async Task<BrowseResponse> BrowseInternalAsync(string username, CancellationToken cancellationToken)
        {
            IMessageConnection connection = null;

            try
            {
                var waitKey = new WaitKey(MessageCode.PeerBrowseResponse, username);
                var browseWait = Waiter.WaitIndefinitely<BrowseResponse>(waitKey, cancellationToken);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);

                connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);
                connection.Disconnected += (sender, message) =>
                {
                    Waiter.Throw(waitKey, new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                };

                var sw = new System.Diagnostics.Stopwatch();
                Diagnostic.Debug($"Sending browse request to peer {username}");
                sw.Start();

                await connection.WriteMessageAsync(new BrowseRequest().ToMessage(), cancellationToken).ConfigureAwait(false);

                var response = await browseWait.ConfigureAwait(false);

                sw.Stop();
                Diagnostic.Debug($"Browse of {username} completed in {sw.ElapsedMilliseconds}ms.  {response.DirectoryCount} directories fetched.");

                return response;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to browse user {username}: {ex.Message}", ex);
            }
        }

        private void ChangeState(SoulseekClientStates state, string message = null)
        {
            var previousState = State;
            State = state;
            StateChanged?.Invoke(this, new SoulseekClientStateChangedEventArgs(previousState, State, message));
        }

        private async Task<byte[]> DownloadInternalAsync(string username, string filename, int token, DownloadOptions options, CancellationToken cancellationToken)
        {
            var download = new Download(username, filename, token, options);
            Task<byte[]> downloadCompleted = null;
            var lastState = DownloadStates.None;

            void UpdateState(DownloadStates state)
            {
                download.State = state;
                var args = new DownloadStateChangedEventArgs(previousState: lastState, download: download);
                lastState = state;
                options.StateChanged?.Invoke(args);
                DownloadStateChanged?.Invoke(this, args);
            }

            void UpdateProgress(int bytesDownloaded)
            {
                var lastBytes = download.BytesDownloaded;
                download.UpdateProgress(bytesDownloaded);
                var eventArgs = new DownloadProgressUpdatedEventArgs(lastBytes, download);
                options.ProgressUpdated?.Invoke(eventArgs);
                DownloadProgressUpdated?.Invoke(this, eventArgs);
            }

            try
            {
                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);
                var peerConnection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);

                Downloads.TryAdd(download.Token, download);

                // prepare two waits; one for the transfer response to confirm that our request is acknowledged and another for the
                // eventual transfer request sent when the peer is ready to send the file. the response message should be returned
                // immediately, while the request will be sent only when we've reached the front of the remote queue.
                var transferRequestAcknowledged = Waiter.Wait<TransferResponse>(
                    new WaitKey(MessageCode.PeerTransferResponse, download.Username, download.Token), null, cancellationToken);
                var transferStartRequested = Waiter.WaitIndefinitely<TransferRequest>(
                    new WaitKey(MessageCode.PeerTransferRequest, download.Username, download.Filename), cancellationToken);

                // request the file
                await peerConnection.WriteMessageAsync(new TransferRequest(TransferDirection.Download, token, filename).ToMessage(), cancellationToken).ConfigureAwait(false);
                UpdateState(DownloadStates.Requested);

                Console.WriteLine($"Waiting for ACK {download.Username} {download.Token}");
                var transferRequestAcknowledgement = await transferRequestAcknowledged.ConfigureAwait(false);

                Console.WriteLine($"Transfer request ACKed");

                if (transferRequestAcknowledgement.Allowed)
                {
                    var tfa = transferRequestAcknowledgement;
                    Console.WriteLine($"Transfer allowed {tfa.Token} {tfa.FileSize}");

                    // the peer is ready to initiate the transfer immediately; we are bypassing their queue. note that only the
                    // legacy client operates this way; SoulseekQt always returns Allowed = false regardless of the current queue.
                    UpdateState(DownloadStates.Initializing);

                    download.Size = transferRequestAcknowledgement.FileSize;

                    // prepare a wait for the overall completion of the download
                    downloadCompleted = Waiter.WaitIndefinitely<byte[]>(download.WaitKey, cancellationToken);

                    // connect to the peer to retrieve the file; for these types of transfers, we must initiate the transfer connection.
                    download.Connection = await PeerConnectionManager
                        .GetTransferConnectionAsync(username, address.IPAddress, address.Port, transferRequestAcknowledgement.Token, cancellationToken)
                        .ConfigureAwait(false);
                }
                else if (transferRequestAcknowledgement.Message.Equals("File not shared.", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new DownloadRejectedException(transferRequestAcknowledgement.Message);
                }
                else
                {
                    Console.WriteLine($"Transfer disallowed");

                    // the download is remotely queued, so put it in the local queue.
                    UpdateState(DownloadStates.Queued);

                    // wait for the peer to respond that they are ready to start the transfer
                    var transferStartRequest = await transferStartRequested.ConfigureAwait(false);

                    var tsr = transferStartRequest;
                    Console.WriteLine($"Start request: token {tsr.Token} filename {tsr.Filename} size: {tsr.FileSize}");

                    download.Size = transferStartRequest.FileSize;
                    download.RemoteToken = transferStartRequest.Token;
                    UpdateState(DownloadStates.Initializing);

                    // wait for both direct and indirect connections, since the official client attempts both types immediately
                    var indirectTransferConnectionInitialized = Waiter.Wait<IConnection>(
                        key: new WaitKey(Constants.WaitKey.IndirectTransfer, download.Username, download.Filename, download.RemoteToken),
                        timeout: Options.PeerConnectionOptions.ConnectTimeout,
                        cancellationToken: cancellationToken);

                    var directTransferConnectionInitialized = Waiter.Wait<IConnection>(
                        key: new WaitKey(Constants.WaitKey.DirectTransfer, download.Username, download.RemoteToken),
                        timeout: Options.PeerConnectionOptions.ConnectTimeout,
                        cancellationToken: cancellationToken);

                    // also prepare a wait for the overall completion of the download
                    downloadCompleted = Waiter.WaitIndefinitely<byte[]>(download.WaitKey, cancellationToken);

                    Console.WriteLine($"Transfer start recieved.  Trying to connect.");

                    // respond to the peer that we are ready to accept the file but first, get a fresh connection (or maybe it's
                    // cached in the manager) to the peer in case it disconnected and was purged while we were waiting.
                    peerConnection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);

                    Console.WriteLine($"Sending transfer response.");
                    await peerConnection.WriteMessageAsync(new TransferResponse(download.RemoteToken, download.Size).ToMessage(), cancellationToken).ConfigureAwait(false);
                    Console.WriteLine($"Response sent.  Waiting for connection...");

                    try
                    {
                        var connectionInitialized = await Task.WhenAny(indirectTransferConnectionInitialized, directTransferConnectionInitialized).ConfigureAwait(false);
                        download.Connection = connectionInitialized.Result;
                    }
                    catch (AggregateException ex)
                    {
                        // todo: write some tests to make sure this surfaces realistic exceptions for different scenarios. bubbling
                        //       an AggregateException here leaks too many implementation details.
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }

                download.Connection.DataRead += (sender, e) => UpdateProgress(e.CurrentLength);
                download.Connection.Disconnected += (sender, message) =>
                {
                    if (download.State.HasFlag(DownloadStates.Succeeded))
                    {
                        Waiter.Complete(download.WaitKey, download.Data);
                    }
                    else if (download.State.HasFlag(DownloadStates.TimedOut))
                    {
                        Waiter.Throw(download.WaitKey, new TimeoutException(message));
                    }
                    else
                    {
                        Waiter.Throw(download.WaitKey, new ConnectionException($"Transfer failed: {message}"));
                    }
                };

                try
                {
                    Console.WriteLine($"Download connection established.  Sending magic bytes...");

                    // this needs to be 16 bytes for transfers beginning immediately, or 8 for queued. not sure what this is; it
                    // was identified via WireShark.
                    await download.Connection.WriteAsync(new byte[16], cancellationToken).ConfigureAwait(false);

                    UpdateState(DownloadStates.InProgress);

                    var bytes = await download.Connection.ReadAsync(download.Size, cancellationToken).ConfigureAwait(false);

                    download.Data = bytes.ToArray();
                    download.State = DownloadStates.Succeeded;

                    download.Connection.Disconnect("Transfer complete.");
                    Diagnostic.Info($"Download of {System.IO.Path.GetFileName(download.Filename)} from {username} complete ({download.Data.Length} of {download.Size} bytes).");
                }
                catch (TimeoutException)
                {
                    download.State = DownloadStates.TimedOut;
                    download.Connection.Disconnect($"Transfer timed out after {Options.TransferConnectionOptions.InactivityTimeout} seconds of inactivity.");
                }
                catch (Exception ex)
                {
                    download.Connection.Disconnect(ex.Message);
                }

                // wait for the download to complete this wait is either completed (on success) or thrown (on anything other than
                // success) in the Disconnected event handler of the transfer connection
                download.Data = await downloadCompleted.ConfigureAwait(false);
                return download.Data;
            }
            catch (DownloadRejectedException ex)
            {
                download.State = DownloadStates.Rejected;
                download.Connection?.Disconnect("Transfer rejected.");

                throw new DownloadException($"Download of file {filename} rejected by user {username}: {ex.Message}", ex);
            }
            catch (OperationCanceledException ex)
            {
                download.State = DownloadStates.Cancelled;
                download.Connection?.Disconnect("Transfer cancelled.");

                throw new DownloadException($"Download of file {filename} from user {username} was cancelled.", ex);
            }
            catch (TimeoutException ex)
            {
                download.State = DownloadStates.TimedOut;
                download.Connection?.Disconnect("Transfer timed out.");

                Diagnostic.Debug(ex.ToString());
                throw new DownloadException($"Failed to download file {filename} from user {username}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                download.State = DownloadStates.Errored;
                download.Connection?.Disconnect("Transfer error.");

                Diagnostic.Debug(ex.ToString());
                throw new DownloadException($"Failed to download file {filename} from user {username}: {ex.Message}", ex);
            }
            finally
            {
                // clean up the wait in case the code threw before it was awaited.
                Waiter.Complete(download.WaitKey);
                Downloads.TryRemove(download.Token, out var _);

                download.Connection?.Dispose();

                // change state so we can fire the progress update a final time with the updated state little bit of a hack to
                // avoid cloning the download
                download.State = DownloadStates.Completed | download.State;
                UpdateProgress(download.Data?.Length ?? 0);
                UpdateState(download.State);
            }
        }

        private async Task<int> GetDownloadPlaceInQueueInternalAsync(string username, string filename, CancellationToken cancellationToken)
        {
            IMessageConnection connection = null;

            try
            {
                var waitKey = new WaitKey(MessageCode.PeerPlaceInQueueResponse, username, filename);
                var responseWait = Waiter.Wait<PeerPlaceInQueueResponse>(waitKey, null, cancellationToken);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);

                connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);
                connection.Disconnected += (sender, message) =>
                {
                    Waiter.Throw(waitKey, new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                };

                await connection.WriteMessageAsync(new PeerPlaceInQueueRequest(filename).ToMessage(), cancellationToken).ConfigureAwait(false);

                var response = await responseWait.ConfigureAwait(false);

                return response.PlaceInQueue;
            }
            catch (Exception ex)
            {
                throw new DownloadPlaceInQueueException($"Failed to fetch place in queue for download of {filename} from {username}: {ex.Message}", ex);
            }
        }

        private async Task<(IPAddress IPAddress, int Port)> GetUserAddressInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var waitKey = new WaitKey(MessageCode.ServerGetPeerAddress, username);
                var addressWait = Waiter.Wait<GetPeerAddressResponse>(waitKey, cancellationToken: cancellationToken);

                await ServerConnection.WriteMessageAsync(new GetPeerAddressRequest(username).ToMessage(), cancellationToken).ConfigureAwait(false);

                var address = await addressWait.ConfigureAwait(false);

                if (address.IPAddress.Equals(IPAddress.Parse("0.0.0.0")))
                {
                    throw new PeerOfflineException($"User {username} appears to be offline.");
                }

                return (address.IPAddress, address.Port);
            }
            catch (Exception ex)
            {
                throw new UserAddressException($"Failed to retrieve address for user {username}: {ex.Message}", ex);
            }
        }

        private async Task<UserInfoResponse> GetUserInfoInternalAsync(string username, CancellationToken cancellationToken)
        {
            IMessageConnection connection = null;

            try
            {
                var waitKey = new WaitKey(MessageCode.PeerInfoResponse, username);
                var infoWait = Waiter.Wait<UserInfoResponse>(waitKey, cancellationToken: cancellationToken);

                var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);

                connection = await PeerConnectionManager.GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken).ConfigureAwait(false);
                connection.Disconnected += (sender, message) =>
                {
                    Waiter.Throw(waitKey, new ConnectionException($"Peer connection disconnected unexpectedly: {message}"));
                };

                await connection.WriteMessageAsync(new UserInfoRequest().ToMessage(), cancellationToken).ConfigureAwait(false);

                var response = await infoWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                throw new UserInfoException($"Failed to retrieve information for user {username}: {ex.Message}", ex);
            }
        }

        private async Task<GetStatusResponse> GetUserStatusInternalAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var getStatusWait = Waiter.Wait<GetStatusResponse>(new WaitKey(MessageCode.ServerGetStatus, username), cancellationToken: cancellationToken);
                await ServerConnection.WriteMessageAsync(new GetStatusRequest(username).ToMessage(), cancellationToken).ConfigureAwait(false);

                var response = await getStatusWait.ConfigureAwait(false);

                return response;
            }
            catch (Exception ex)
            {
                throw new UserStatusException($"Failed to retrieve status for user {Username}: {ex.Message}", ex);
            }
        }

        private async Task LoginInternalAsync(string username, string password, CancellationToken cancellationToken)
        {
            try
            {
                var loginWait = Waiter.Wait<LoginResponse>(new WaitKey(MessageCode.ServerLogin), cancellationToken: cancellationToken);

                await ServerConnection.WriteMessageAsync(new LoginRequest(username, password).ToMessage(), cancellationToken).ConfigureAwait(false);

                var response = await loginWait.ConfigureAwait(false);

                if (Options.ListenPort.HasValue)
                {
                    await ServerConnection.WriteMessageAsync(new SetListenPortRequest(Options.ListenPort.Value).ToMessage(), cancellationToken).ConfigureAwait(false);
                }

                if (response.Succeeded)
                {
                    Username = username;
                    ChangeState(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                }
                else
                {
                    Disconnect($"The server rejected login attempt: {response.Message}"); // upon login failure the server will refuse to allow any more input, eventually disconnecting.
                    throw new LoginException($"The server rejected login attempt: {response.Message}");
                }
            }
            catch (Exception ex)
            {
                throw new LoginException($"Failed to log in as {username}: {ex.Message}", ex);
            }
        }

        public Task UploadAsync(string username, string filename, byte[] data, int? token = null, CancellationToken? cancellationToken = null)
        {
            token = token ?? GetNextToken();
            return UploadInternalAsync(username, filename, data, (int)token, cancellationToken ?? CancellationToken.None);
        }

        private async Task UploadInternalAsync(string username, string filename, byte[] data, int token, CancellationToken cancellationToken)
        {
            var tstart = new TransferRequest(TransferDirection.Upload, token, filename, data.Length);

            var address = await GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);

            var connection = await PeerConnectionManager
                .GetOrAddMessageConnectionAsync(username, address.IPAddress, address.Port, cancellationToken)
                .ConfigureAwait(false);

            Console.WriteLine($"Writing transfer request");
            await connection.WriteMessageAsync(tstart.ToMessage()).ConfigureAwait(false);

            Console.WriteLine($"Waiting for transfer response");
            //// here we wait for the peer to respond that they are ready to accept the file
            var res = await Waiter.Wait<TransferResponse>(new WaitKey(MessageCode.PeerTransferResponse, connection.Username, token)).ConfigureAwait(false);

            Console.WriteLine($"Getting transfer connection...");
            // completed in the response handler above, we're now ready to connect and send the file.
            var ttransferConnection = await PeerConnectionManager
                .GetTransferConnectionAsync(connection.Username, connection.IPAddress, connection.Port, token)
                .ConfigureAwait(false);

            // we know from the download logic that each transfer begins with some number of magic bytes, either 8 or 16.
            // read those, then continue.
            // todo: inspect this and figure out what this data is, understand how many bytes are actually written and under what conditions
            //await ttransferConnection.WriteAsync(BitConverter.GetBytes(res.Token)).ConfigureAwait(false);

            // write the file to the connection.
            //Console.WriteLine($"Magic bytes read.  Writing 'file'");
            Console.WriteLine($"Writing file");
            await ttransferConnection.WriteAsync(data)
                .ConfigureAwait(false);
        }

        private async void PeerConnection_MessageRead(object sender, Message message)
        {
            var connection = (IMessageConnection)sender;
            Diagnostic.Debug($"Peer message received: {message.Code} from {connection.Username} ({connection.IPAddress}:{connection.Port})");

            try
            {
                switch (message.Code)
                {
                    case MessageCode.PeerSearchResponse:
                        var searchResponse = SearchResponseSlim.Parse(message);
                        if (Searches.TryGetValue(searchResponse.Token, out var search))
                        {
                            search.AddResponse(searchResponse);
                        }

                        break;

                    case MessageCode.PeerBrowseResponse:
                        var browseWaitKey = new WaitKey(MessageCode.PeerBrowseResponse, connection.Username);
                        try
                        {
                            Waiter.Complete(browseWaitKey, BrowseResponse.Parse(message));
                        }
                        catch (Exception ex)
                        {
                            Waiter.Throw(browseWaitKey, new MessageReadException("The peer returned an invalid browse response.", ex));
                            throw;
                        }

                        break;

                    case MessageCode.PeerInfoResponse:
                        var infoResponse = UserInfoResponse.Parse(message);
                        Waiter.Complete(new WaitKey(MessageCode.PeerInfoResponse, connection.Username), infoResponse);
                        break;

                    case MessageCode.PeerTransferResponse:
                        var transferResponse = TransferResponse.Parse(message);
                        Console.WriteLine($"Got response from {connection.Username}: {transferResponse.Token}");
                        Waiter.Complete(new WaitKey(MessageCode.PeerTransferResponse, connection.Username, transferResponse.Token), transferResponse);
                        break;

                    case MessageCode.PeerQueueDownload:
                        // the end state here is to wait until there's actually a free slot, then send this request to the peer to let them know we are ready to start the actual
                        // transfer.
                        var queueDownloadRequest = QueueDownloadRequest.Parse(message);
                        var file = $"The quick brown fox jumps over the lazy dog {System.IO.Path.GetFileName(queueDownloadRequest.Filename)}";
                        await UploadAsync(connection.Username, queueDownloadRequest.Filename, Encoding.ASCII.GetBytes(file))
                            .ConfigureAwait(false);

                        break;

                    case MessageCode.PeerTransferRequest:
                        var transferRequest = TransferRequest.Parse(message);

                        if (transferRequest.Direction == TransferDirection.Upload)
                        {
                            Waiter.Complete(new WaitKey(MessageCode.PeerTransferRequest, connection.Username, transferRequest.Filename), transferRequest);
                        }
                        else
                        {
                            // this is sent when a peer requests a file, i think only when it has identified that slots are available.
                            Console.WriteLine($"Transfer request from {connection.Username}: direction: {transferRequest.Direction} {transferRequest.Token} {transferRequest.Filename}");

                            // the official client seems to respond to every request like this with "queued", regardless of whether slots are available, so we'll do the same.
                            var response = new TransferResponse(transferRequest.Token, 43); // todo: verify the message
                            await connection.WriteMessageAsync(response.ToMessage()).ConfigureAwait(false);

                            // the end state here is to wait until there's actually a free slot, then send this request to the peer to let them know we are ready to start the actual
                            // transfer.
                            var start = new TransferRequest(TransferDirection.Upload, transferRequest.Token, transferRequest.Filename, 100000);
                            await connection.WriteMessageAsync(start.ToMessage()).ConfigureAwait(false);

                            // here we wait for the peer to respond that they are ready to accept the file
                            Console.WriteLine($"Waiting for transfer response....");
                            await Waiter.Wait<TransferResponse>(new WaitKey(MessageCode.PeerTransferResponse, connection.Username, transferRequest.Token)).ConfigureAwait(false);

                            var transferConnection = await PeerConnectionManager
                                .GetTransferConnectionAsync(connection.Username, connection.IPAddress, connection.Port, transferRequest.Token)
                                .ConfigureAwait(false);

                            // we know from the download logic that each transfer begins with some number of magic bytes, either 8 or 16.
                            // read those, then continue.
                            // todo: inspect this and figure out what this data is, understand how many bytes are actually written and under what conditions
                            Console.WriteLine($"Transfer connection established. Trying to read magic bytes");
                            var magic = await transferConnection.ReadAsync(8).ConfigureAwait(false);
                            Console.WriteLine($"Magic bytes read: {BitConverter.ToInt64(magic, 0)}  Writing 'file'");

                            await transferConnection.WriteAsync(Encoding.ASCII.GetBytes(new string('a', 43)))
                                .ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.PeerQueueFailed:
                        var queueFailedResponse = PeerQueueFailedResponse.Parse(message);
                        Waiter.Throw(new WaitKey(MessageCode.PeerTransferRequest, connection.Username, queueFailedResponse.Filename), new DownloadRejectedException(queueFailedResponse.Message));
                        break;

                    case MessageCode.PeerPlaceInQueueResponse:
                        var placeInQueueResponse = PeerPlaceInQueueResponse.Parse(message);
                        Waiter.Complete(new WaitKey(MessageCode.PeerPlaceInQueueResponse, connection.Username, placeInQueueResponse.Filename), placeInQueueResponse);
                        break;

                    case MessageCode.PeerUploadFailed:
                        var uploadFailedResponse = PeerUploadFailedResponse.Parse(message);
                        var msg = $"Download of {uploadFailedResponse.Filename} reported as failed by {connection.Username}.";

                        var download = Downloads.Values.FirstOrDefault(d => d.Username == connection.Username && d.Filename == uploadFailedResponse.Filename);
                        if (download != null)
                        {
                            Waiter.Throw(new WaitKey(MessageCode.PeerTransferRequest, download.Username, download.Filename), new DownloadException(msg));
                            Waiter.Throw(download.WaitKey, new DownloadException(msg));
                        }

                        Diagnostic.Debug(msg);
                        break;

                    case MessageCode.PeerBrowseRequest:
                        var browseResponse = Resolvers.BrowseResponse(connection.Username, connection.IPAddress, connection.Port);
                        await connection.WriteMessageAsync(browseResponse.ToMessage()).ConfigureAwait(false);
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled peer message: {message.Code} from {connection.Username} ({connection.IPAddress}:{connection.Port}); {message.Payload.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling peer message: {message.Code} from {connection.Username} ({connection.IPAddress}:{connection.Port}); {ex.Message}", ex);
            }
        }

        private async Task<IReadOnlyCollection<SearchResponse>> SearchInternalAsync(string searchText, int token, SearchOptions options, CancellationToken cancellationToken)
        {
            var search = new Search(searchText, token, options);
            var lastState = SearchStates.None;

            void UpdateState(SearchStates state)
            {
                search.State = state;
                var args = new SearchStateChangedEventArgs(previousState: lastState, search: search);
                lastState = state;
                options.StateChanged?.Invoke(args);
                SearchStateChanged?.Invoke(this, args);
            }

            try
            {
                search.ResponseReceived = (response) =>
                {
                    var eventArgs = new SearchResponseReceivedEventArgs(response, search);
                    options.ResponseReceived?.Invoke(eventArgs);
                    SearchResponseReceived?.Invoke(this, eventArgs);
                };

                Searches.TryAdd(search.Token, search);
                UpdateState(SearchStates.Requested);

                await ServerConnection.WriteMessageAsync(new SearchRequest(search.SearchText, search.Token).ToMessage(), cancellationToken).ConfigureAwait(false);
                UpdateState(SearchStates.InProgress);

                var responses = await search.WaitForCompletion(cancellationToken).ConfigureAwait(false);
                return responses.ToList().AsReadOnly();
            }
            catch (OperationCanceledException ex)
            {
                search.Complete(SearchStates.Cancelled);
                throw new SearchException($"Search for {searchText} ({token}) was cancelled.", ex);
            }
            catch (Exception ex)
            {
                search.Complete(SearchStates.Errored);
                throw new SearchException($"Failed to search for {searchText} ({token}): {ex.Message}", ex);
            }
            finally
            {
                Searches.TryRemove(search.Token, out _);

                UpdateState(SearchStates.Completed | search.State);
                search.Dispose();
            }
        }

        private async Task SendPrivateMessageInternalAsync(string username, string message, CancellationToken cancellationToken)
        {
            try
            {
                await ServerConnection.WriteMessageAsync(new PrivateMessageRequest(username, message).ToMessage(), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new PrivateMessageException($"Failed to send private message to user {username}: {ex.Message}", ex);
            }
        }

        private void ServerConnection_Disconnected(object sender, string e)
        {
            Disconnect(e);
        }

        private async void ServerConnection_MessageRead(object sender, Message message)
        {
            Diagnostic.Debug($"Server message received: {message.Code}");

            try
            {
                switch (message.Code)
                {
                    case MessageCode.ServerParentMinSpeed:
                    case MessageCode.ServerParentSpeedRatio:
                    case MessageCode.ServerWishlistInterval:
                        Waiter.Complete(new WaitKey(message.Code), IntegerResponse.Parse(message));
                        break;

                    case MessageCode.ServerLogin:
                        Waiter.Complete(new WaitKey(message.Code), LoginResponse.Parse(message));
                        break;

                    case MessageCode.ServerRoomList:
                        Waiter.Complete(new WaitKey(message.Code), RoomList.Parse(message));
                        break;

                    case MessageCode.ServerPrivilegedUsers:
                        Waiter.Complete(new WaitKey(message.Code), PrivilegedUserList.Parse(message));
                        break;

                    case MessageCode.ServerConnectToPeer:
                        var connectToPeerResponse = ConnectToPeerResponse.Parse(message);

                        if (connectToPeerResponse.Type == Constants.ConnectionType.Tranfer)
                        {
                            // ensure that we are expecting at least one file from this user before we connect. the response
                            // doesn't contain any other identifying information about the file.
                            if (!Downloads.IsEmpty && Downloads.Values.Any(d => d.Username == connectToPeerResponse.Username))
                            {
                                var (connection, remoteToken) = await PeerConnectionManager.AddTransferConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                                var download = Downloads.Values.FirstOrDefault(v => v.RemoteToken == remoteToken && v.Username == connectToPeerResponse.Username);

                                if (download != default(Download))
                                {
                                    Waiter.Complete(new WaitKey(Constants.WaitKey.IndirectTransfer, download.Username, download.Filename, download.RemoteToken), connection);
                                }
                            }
                            else
                            {
                                throw new SoulseekClientException($"Unexpected transfer request from {connectToPeerResponse.Username} ({connectToPeerResponse.IPAddress}:{connectToPeerResponse.Port}); Ignored.");
                            }
                        }
                        else
                        {
                            await PeerConnectionManager.GetOrAddMessageConnectionAsync(connectToPeerResponse).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.ServerAddUser:
                        var addUserResponse = AddUserResponse.Parse(message);
                        Waiter.Complete(new WaitKey(message.Code, addUserResponse.Username), addUserResponse);
                        break;

                    case MessageCode.ServerGetStatus:
                        var statsResponse = GetStatusResponse.Parse(message);
                        Waiter.Complete(new WaitKey(message.Code, statsResponse.Username), statsResponse);
                        UserStatusChanged?.Invoke(this, new UserStatusChangedEventArgs(statsResponse));
                        break;

                    case MessageCode.ServerPrivateMessage:
                        var pm = PrivateMessage.Parse(message);
                        PrivateMessageReceived?.Invoke(this, pm);

                        if (Options.AutoAcknowledgePrivateMessages)
                        {
                            await AcknowledgePrivateMessageInternalAsync(pm.Id, CancellationToken.None).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.ServerGetPeerAddress:
                        var peerAddressResponse = GetPeerAddressResponse.Parse(message);
                        Waiter.Complete(new WaitKey(message.Code, peerAddressResponse.Username), peerAddressResponse);
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled server message: {message.Code}; {message.Payload.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling server message: {message.Code}; {ex.Message}", ex);
            }
        }
    }
}