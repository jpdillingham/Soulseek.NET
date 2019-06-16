// <copyright file="PeerConnectionManager.cs" company="JP Dillingham">
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
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Messaging.Tcp;
    using Soulseek.Tcp;

    /// <summary>
    ///     Manages peer <see cref="IConnection"/> instances for the application.
    /// </summary>
    internal sealed class PeerConnectionManager : IPeerConnectionManager
    {
        private int waitingMessageConnections = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerConnectionManager"/> class.
        /// </summary>
        /// <param name="connectionFactory">The IConnectionFactory instance to use.</param>
        internal PeerConnectionManager(
            IListener listener,
            IWaiter waiter,
            ISoulseekClient soulseekClient,
            EventHandler<Message> messageHandler,
            int concurrentMessageConnectionLimit = 1000,
            IConnectionFactory connectionFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            if (concurrentMessageConnectionLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrentMessageConnectionLimit), $"Concurrent message connection limit must be greater than zero.");
            }

            ConcurrentMessageConnectionLimit = concurrentMessageConnectionLimit;

            SoulseekClient = soulseekClient;
            MessageSemaphore = new SemaphoreSlim(ConcurrentMessageConnectionLimit, ConcurrentMessageConnectionLimit);
            MessageConnections = new ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();

            TransferConnections = new ConcurrentDictionary<(ConnectionKey Key, int Token), IConnection>();

            Waiter = waiter ?? new Waiter(SoulseekClient.Options.MessageTimeout);

            Listener = listener;
            Listener.Accepted += Listener_Accepted;

            MessageHandler = messageHandler;
            ConnectionFactory = connectionFactory ?? new ConnectionFactory();
            Diagnostic = diagnosticFactory ?? new DiagnosticFactory(this, SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Gets the number of active peer message connections.
        /// </summary>
        public int ActiveMessageConnections => MessageConnections.Count;

        /// <summary>
        ///     Gets the number of active transfer connections.
        /// </summary>
        public int ActiveTransferConnections => TransferConnections.Count;

        /// <summary>
        ///     Gets the number of allowed concurrent peer message connections.
        /// </summary>
        public int ConcurrentMessageConnectionLimit { get; }

        /// <summary>
        ///     Gets the number of waiting peer message connections.
        /// </summary>
        public int WaitingMessageConnections => waitingMessageConnections;

        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }
        private IListener Listener { get; }
        private ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)> MessageConnections { get; set; }
        private EventHandler<Message> MessageHandler { get; }
        private SemaphoreSlim MessageSemaphore { get; }
        private ConcurrentDictionary<int, string> PendingSolicitedConnections { get; set; } = new ConcurrentDictionary<int, string>();
        private ISoulseekClient SoulseekClient { get; }
        private ConcurrentDictionary<(ConnectionKey Key, int Token), IConnection> TransferConnections { get; set; }
        private IWaiter Waiter { get; }

        /// <summary>
        ///     Adds a new transfer <see cref="IConnection"/> from an incoming direct connection.
        /// </summary>
        /// <param name="ipAddress">The IP address of the connection.</param>
        /// <param name="port">The port of the connection.</param>
        /// <param name="token">The transfer token.</param>
        /// <param name="tcpClient">The TCP client for the established connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <returns>The new connection.</returns>
        public IConnection AddDirectTransferConnection(IPAddress ipAddress, int port, int token, ITcpClient tcpClient, ConnectionOptions options)
        {
            var connection = new Connection(ipAddress, port, options, tcpClient);
            connection.Disconnected += (sender, e) => TransferConnections.TryRemove((connection.Key, token), out _);

            TransferConnections.AddOrUpdate((connection.Key, token), connection, (k, v) => connection);
            return connection;
        }

        /// <summary>
        ///     Gets an existing peer <see cref="IMessageConnection"/>, or adds and initialized a new instance if one does not exist.
        /// </summary>
        /// <remarks>A solicited connection is one which is initiated remotely by sending a <see cref="ConnectToPeerResponse"/>.</remarks>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The existing or new connection.</returns>
        public async Task<IMessageConnection> AddOrUpdateMessageConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            var key = new ConnectionKey(connectToPeerResponse.Username, connectToPeerResponse.IPAddress, connectToPeerResponse.Port, MessageConnectionType.Peer);
            IMessageConnection connection = null;

            // get or add a connection. we only care about the semphore at this point, so discard the connection.
            var (semaphore, _) = await GetOrAddMessageConnectionRecordAsync(key.Username).ConfigureAwait(false);

            // await the semaphore we got back to ensure exclusive access over the code that follows. this is important because
            // while the GetOrAdd above either gets or retrieves a connection in a thread safe manner (through
            // ConcurrentDictionary), the connection itself is not synchronized.
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                // retrieve the connection now that we have exclusive access to the record.
                (_, connection) = await GetOrAddMessageConnectionRecordAsync(key.Username).ConfigureAwait(false);

                // the connection is null when added, so if it is no longer null then it was either already established prior to
                // this method being invoked, or has been established by another thread between the first and second calls to
                // GetOrAddMessageConnectionAsync(). either way, return it as is.
                if (connection != null)
                {
                    return connection;
                }
                else
                {
                    // establish the connection.
                    connection = ConnectionFactory.GetMessageConnection(MessageConnectionType.Peer, connectToPeerResponse.Username, connectToPeerResponse.IPAddress, connectToPeerResponse.Port, SoulseekClient.Options.PeerConnectionOptions);
                    connection.Context = connectToPeerResponse;

                    connection.MessageRead += MessageHandler;
                    connection.Disconnected += (sender, e) => RemoveMessageConnectionRecord(connection);

                    await connection.ConnectAsync().ConfigureAwait(false);

                    // update the dictionary to replace the null value with the new connection. if the record was removed between
                    // the previous call to GetOrAddMessageConnectionAsync(), a new record is inserted with the existing semaphore.
                    MessageConnections.AddOrUpdate(key.Username, (semaphore, connection), (k, v) => (v.Semaphore, connection));

                    var request = new PierceFirewallRequest(connectToPeerResponse.Token).ToMessage();
                    await connection.WriteAsync(request.ToByteArray()).ConfigureAwait(false);
                }
            }
            finally
            {
                semaphore.Release();
            }

            return connection;
        }

        /// <summary>
        ///     Adds a new transfer <see cref="IConnection"/> and pierces the firewall.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests while the connection is connecting.</param>
        /// <returns>The new connection.</returns>
        public async Task<IConnection> AddTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse, CancellationToken? cancellationToken = null)
        {
            var connection = ConnectionFactory.GetConnection(connectToPeerResponse.IPAddress, connectToPeerResponse.Port, SoulseekClient.Options.TransferConnectionOptions);
            connection.Disconnected += (sender, e) => TransferConnections.TryRemove((connection.Key, connectToPeerResponse.Token), out _);

            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

            TransferConnections.AddOrUpdate((connection.Key, connectToPeerResponse.Token), connection, (k, v) => connection);

            Console.WriteLine($"Sending PierceFirewall with token {connectToPeerResponse.Token}");
            var request = new PierceFirewallRequest(connectToPeerResponse.Token);
            await connection.WriteAsync(request.ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);

            return connection;
        }

        /// <summary>
        ///     Adds a new transfer <see cref="IConnection"/> and sends a peer init request.
        /// </summary>
        /// <param name="connectionKey">The connection key, comprised of the remote IP address and port.</param>
        /// <param name="token">The transfer token.</param>
        /// <param name="localUsername">The username of the local user, required to initiate the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests while the connection is connecting.</param>
        /// <returns>The new connection.</returns>
        public async Task<IConnection> AddUnsolicitedTransferConnectionAsync(ConnectionKey connectionKey, int token, ConnectionOptions options, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetConnection(connectionKey.IPAddress, connectionKey.Port, options);
            connection.Disconnected += (sender, e) => TransferConnections.TryRemove((connection.Key, token), out _);

            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

            TransferConnections.AddOrUpdate((connection.Key, token), connection, (k, v) => connection);

            Console.WriteLine($"Sending PeerInit type 'F' with token {token} to {connectionKey.Username}");
            await connection.WriteAsync(new PeerInitRequest(SoulseekClient.Username, Constants.ConnectionType.Tranfer, token).ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);

            return connection;
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<IMessageConnection> GetOrAddMessageConnectionAsync(string username, CancellationToken cancellationToken)
        {
            IMessageConnection connection = null;

            var (semaphore, _) = await GetOrAddMessageConnectionRecordAsync(username).ConfigureAwait(false);
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                (_, connection) = await GetOrAddMessageConnectionRecordAsync(username).ConfigureAwait(false);

                if (connection != null)
                {
                    return connection;
                }
                else
                {
                    var address = await SoulseekClient.GetUserAddressAsync(username, cancellationToken).ConfigureAwait(false);
                    var connectionKey = new ConnectionKey(username, address.IPAddress, address.Port, MessageConnectionType.Peer);

                    try
                    {
                        Diagnostic.Debug($"Attempting direct connection to {username} ({address.IPAddress}:{address.Port})");

                        connection = await GetOutboundDirectMessageConnectionAsync(connectionKey, MessageHandler, SoulseekClient.Options.PeerConnectionOptions, cancellationToken).ConfigureAwait(false);
                        MessageConnections.AddOrUpdate(connectionKey.Username, (new SemaphoreSlim(1, 1), connection), (k, v) => (v.Semaphore, connection));

                        Diagnostic.Debug($"Direct connection to {username} ({address.IPAddress}:{address.Port}) established.");
                        return connection;
                    }
                    catch
                    {
                        try
                        {
                            Diagnostic.Debug($"Direct connection to {username} ({address.IPAddress}:{address.Port}) failed.  Attempting indirect connection.");

                            connection = await GetIndirectMessageConnection(username, cancellationToken).ConfigureAwait(false);
                            MessageConnections.AddOrUpdate(connectionKey.Username, (new SemaphoreSlim(1, 1), connection), (k, v) => (v.Semaphore, connection));

                            Diagnostic.Debug($"Indirect connection to {username} ({address.IPAddress}:{address.Port}) established.");
                            return connection;
                        }
                        catch
                        {
                            var msg = $"Failed to establish connection to {username} ({address.IPAddress}:{address.Port})";
                            Diagnostic.Warning(msg);
                            throw new ConnectionException(msg);
                        }
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        ///     Disposes and removes all active and queued connections.
        /// </summary>
        public void RemoveAndDisposeAll()
        {
            while (!MessageConnections.IsEmpty)
            {
                if (MessageConnections.TryRemove(MessageConnections.Keys.First(), out var value))
                {
                    value.Semaphore?.Dispose();
                    value.Connection?.Dispose();
                }
            }

            TransferConnections.RemoveAndDisposeAll();
        }

        private async Task<IMessageConnection> AddInboundMessageConnectionAsync(string username, IPAddress ipAddress, int port, ITcpClient tcpClient)
        {
            var connectionKey = new ConnectionKey(username, ipAddress, port, MessageConnectionType.Peer);
            var connection = ConnectionFactory.GetMessageConnection(MessageConnectionType.Peer, connectionKey.Username, connectionKey.IPAddress, connectionKey.Port, SoulseekClient.Options.PeerConnectionOptions, tcpClient);

            connection.MessageRead += MessageHandler;
            connection.Disconnected += (sender, e) => RemoveMessageConnectionRecord(connection);

            var (semaphore, _) = await GetOrAddMessageConnectionRecordAsync(connectionKey.Username).ConfigureAwait(false);
            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                MessageConnections.AddOrUpdate(connectionKey.Username, (new SemaphoreSlim(1, 1), connection), (k, v) => (v.Semaphore, connection));
                Diagnostic.Debug($"Inbound connection to {username} ({connection.IPAddress}:{connection.Port}) established.");
            }
            finally
            {
                semaphore.Release();
            }

            return connection;
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    RemoveAndDisposeAll();
                    MessageSemaphore.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task<IMessageConnection> GetIndirectMessageConnection(string username, CancellationToken cancellationToken)
        {
            var token = SoulseekClient.GetNextToken();

            try
            {
                PendingSolicitedConnections.TryAdd(token, username);

                await ((SoulseekClient)SoulseekClient).ServerConnection
                    .WriteMessageAsync(new ConnectToPeerRequest(token, username, Constants.ConnectionType.Peer).ToMessage(), cancellationToken)
                    .ConfigureAwait(false);

                var connection = await Waiter.Wait<IMessageConnection>(new WaitKey(Constants.WaitKey.SolicitedConnection, username, token), null, cancellationToken).ConfigureAwait(false);

                MessageConnections.AddOrUpdate(username, (new SemaphoreSlim(1, 1), connection), (k, v) => (v.Semaphore, connection));
                return connection;
            }
            finally
            {
                PendingSolicitedConnections.TryRemove(token, out var _);
            }
        }

        private async Task<(SemaphoreSlim Semaphore, IMessageConnection Connection)> GetOrAddMessageConnectionRecordAsync(string username)
        {
            if (MessageConnections.TryGetValue(username, out var record))
            {
                return record;
            }

            Interlocked.Increment(ref waitingMessageConnections);
            await MessageSemaphore.WaitAsync().ConfigureAwait(false);
            Interlocked.Decrement(ref waitingMessageConnections);

            record = MessageConnections.GetOrAdd(username, (k) => (new SemaphoreSlim(1, 1), null));

            if (record.Connection == null)
            {
                Diagnostic.Debug($"Initialized message connection to {username}");
            }

            return record;
        }

        private async Task<IMessageConnection> GetOutboundDirectMessageConnectionAsync(ConnectionKey connectionKey, EventHandler<Message> messageHandler, ConnectionOptions options, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetMessageConnection(MessageConnectionType.Peer, connectionKey.Username, connectionKey.IPAddress, connectionKey.Port, options);
            connection.MessageRead += messageHandler;
            connection.Disconnected += (sender, e) => RemoveMessageConnectionRecord(connection);

            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
            await connection.WriteAsync(new PeerInitRequest(SoulseekClient.Username, Constants.ConnectionType.Peer, SoulseekClient.GetNextToken()).ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);

            return connection;
        }

        private async void Listener_Accepted(object sender, IConnection connection)
        {
            Diagnostic.Info($"Accepted incoming connection from {connection.IPAddress}:{Listener.Port}");

            try
            {
                var lengthBytes = await connection.ReadAsync(4).ConfigureAwait(false);
                var length = BitConverter.ToInt32(lengthBytes, 0);

                var bodyBytes = await connection.ReadAsync(length).ConfigureAwait(false);
                byte[] message = lengthBytes.Concat(bodyBytes).ToArray();

                if (PeerInitResponse.TryParse(message, out var peerInit))
                {
                    // this connection is the result of an unsolicited connection from the remote peer, either to request info or
                    // browse, or to send a file.
                    Diagnostic.Debug($"PeerInit for transfer type {peerInit.TransferType} received from {peerInit.Username} ({connection.IPAddress}:{Listener.Port})");

                    if (peerInit.TransferType == Constants.ConnectionType.Peer)
                    {
                        await AddInboundMessageConnectionAsync(
                            peerInit.Username,
                            connection.IPAddress,
                            Listener.Port,
                            connection.HandoffTcpClient()).ConfigureAwait(false);
                    }
                    else if (peerInit.TransferType == Constants.ConnectionType.Tranfer)
                    {
                        var cconnection = AddDirectTransferConnection(
                            connection.IPAddress,
                            Listener.Port,
                            peerInit.Token,
                            connection.HandoffTcpClient(),
                            null);

                        Console.WriteLine($"Completing DT wait for {peerInit.Username}");
                        Waiter.Complete(new WaitKey(Constants.WaitKey.DirectTransfer, peerInit.Username), cconnection);
                    }
                }
                else if (PierceFirewallResponse.TryParse(message, out var pierceFirewall))
                {
                    // this connection is the result of a ConnectToPeer request sent to the user, and the incoming message will
                    // contain the token that was provided in the request. Ensure this token is among those expected, and use it to
                    // determine the username of the remote user.
                    if (PendingSolicitedConnections.TryGetValue(pierceFirewall.Token, out var username))
                    {
                        Diagnostic.Debug($"PierceFirewall with token {pierceFirewall.Token} received from {username} ({connection.IPAddress}:{Listener.Port})");

                        var messageConnection = ConnectionFactory
                            .GetMessageConnection(MessageConnectionType.Peer, username, connection.IPAddress, connection.Port, SoulseekClient.Options.PeerConnectionOptions, connection.HandoffTcpClient());

                        messageConnection.MessageRead += MessageHandler;
                        messageConnection.Disconnected += (s, e) => RemoveMessageConnectionRecord(messageConnection);

                        Waiter.Complete(new WaitKey(Constants.WaitKey.SolicitedConnection, username, pierceFirewall.Token), messageConnection);
                    }
                }
                else
                {
                    Diagnostic.Warning($"Unknown direct connection type from {connection.IPAddress}:{connection.Port}");
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Failed to initialize direct connection from {connection.IPAddress}:{connection.Port}: {ex.Message}");
                connection.Disconnect(ex.Message);
                connection.Dispose();
            }
        }

        private void RemoveMessageConnectionRecord(IMessageConnection connection)
        {
            if (MessageConnections.TryRemove(connection.Key.Username, out _))
            {
                Diagnostic.Debug($"Removing message connection to {connection.Key.Username} ({connection.IPAddress}:{connection.Port})");

                // only release if we successfully removed a connection. this can throw if another thread released it first and the
                // semaphore tries to release more than its capacity.
                MessageSemaphore.Release();
            }
        }
    }
}