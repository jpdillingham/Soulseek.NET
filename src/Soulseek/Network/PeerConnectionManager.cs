// <copyright file="PeerConnectionManager.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Exceptions;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;
    using Soulseek.Options;

    /// <summary>
    ///     Manages peer <see cref="IConnection"/> instances for the application.
    /// </summary>
    internal sealed class PeerConnectionManager : IPeerConnectionManager
    {
        private int waitingMessageConnections = 0;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerConnectionManager"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="connectionFactory">The IConnectionFactory instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        internal PeerConnectionManager(
            SoulseekClient soulseekClient,
            IConnectionFactory connectionFactory = null,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));

            ConcurrentMessageConnectionLimit = SoulseekClient?.Options?.ConcurrentPeerMessageConnectionLimit
                ?? new ClientOptions().ConcurrentPeerMessageConnectionLimit;

            ConnectionFactory = connectionFactory ?? new ConnectionFactory();

            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new ClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));

            MessageSemaphore = new SemaphoreSlim(ConcurrentMessageConnectionLimit, ConcurrentMessageConnectionLimit);
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        /// <summary>
        ///     Gets the number of allowed concurrent peer message connections.
        /// </summary>
        public int ConcurrentMessageConnectionLimit { get; }

        /// <summary>
        ///     Gets current list of peer message connections.
        /// </summary>
        public IReadOnlyCollection<(string Username, IPAddress IPAddress, int Port)> MessageConnections =>
            MessageConnectionDictionary.Values.Select(r => (r.Connection.Username, r.Connection.IPAddress, r.Connection.Port)).ToList().AsReadOnly();

        /// <summary>
        ///     Gets a dictionary containing the pending connection solicitations.
        /// </summary>
        public IReadOnlyDictionary<int, string> PendingSolicitations => new ReadOnlyDictionary<int, string>(PendingSolicitationDictionary);

        /// <summary>
        ///     Gets the number of waiting peer message connections.
        /// </summary>
        public int WaitingMessageConnections => waitingMessageConnections;

        private IConnectionFactory ConnectionFactory { get; }
        private IDiagnosticFactory Diagnostic { get; }
        private bool Disposed { get; set; }

        private ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)> MessageConnectionDictionary { get; set; } =
            new ConcurrentDictionary<string, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();

        private SemaphoreSlim MessageSemaphore { get; }
        private CancellationTokenSource MessageSemaphoreCancellationTokenSource { get; } = new CancellationTokenSource();
        private ConcurrentDictionary<int, string> PendingSolicitationDictionary { get; set; } = new ConcurrentDictionary<int, string>();
        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Adds a new message connection from an incoming connection.
        /// </summary>
        /// <param name="username">The username from which the connection originated.</param>
        /// <param name="tcpClient">The TcpClient handling the accepted connection.</param>
        /// <returns>The operation context.</returns>
        public async Task AddMessageConnectionAsync(string username, ITcpClient tcpClient)
        {
            var endpoint = tcpClient.RemoteEndPoint;

            var connection = ConnectionFactory.GetMessageConnection(
                username,
                endpoint.Address,
                endpoint.Port,
                SoulseekClient.Options.PeerConnectionOptions,
                tcpClient);

            connection.MessageRead += SoulseekClient.PeerMessageHandler.HandleMessage;
            connection.Disconnected += MessageConnection_Disconnected;
            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Unsolicited inbound message connection to {username} ({endpoint.Address}:{endpoint.Port}) disconnected.");

            connection.StartReadingContinuously();

            var (semaphore, _) = await GetOrAddMessageConnectionRecordAsync(username).ConfigureAwait(false);
            await semaphore.WaitAsync(MessageSemaphoreCancellationTokenSource.Token).ConfigureAwait(false);

            try
            {
                AddOrUpdateMessageConnectionRecord(username, connection);
            }
            finally
            {
                semaphore.Release();
            }

            Diagnostic.Debug($"Unsolicited inbound connection to {username} ({connection.IPAddress}:{connection.Port}) established.");
        }

        /// <summary>
        ///     Adds a new transfer connection from an incoming connection.
        /// </summary>
        /// <param name="username">The username from which the connection originated.</param>
        /// <param name="token">The token with which the firewall was pierced.</param>
        /// <param name="tcpClient">The TcpClient handling the accepted connection.</param>
        /// <returns>The operation context.</returns>
        public async Task AddTransferConnectionAsync(string username, int token, ITcpClient tcpClient)
        {
            var endpoint = tcpClient.RemoteEndPoint;

            var connection = ConnectionFactory.GetConnection(
                endpoint.Address,
                endpoint.Port,
                SoulseekClient.Options.TransferConnectionOptions,
                tcpClient);

            // bind a temporary handler so we can get diagnostics for disconnects before the permanent handler is bound below
            void HandleDisconnect(object sender, string message) => Diagnostic.Debug($"Unsolicited inbound transfer connection to {username} ({endpoint.Address}:{endpoint.Port}) for token {token} disconnected.");
            connection.Disconnected += HandleDisconnect;

            int remoteToken;

            try
            {
                var remoteTokenBytes = await connection.ReadAsync(4).ConfigureAwait(false);
                remoteToken = BitConverter.ToInt32(remoteTokenBytes, 0);
            }
            catch (Exception)
            {
                connection.Dispose();
                throw;
            }

            connection.Disconnected += (sender, e) =>
            {
                Diagnostic.Debug($"Unsolicited inbound transfer connection to {username} ({endpoint.Address}:{endpoint.Port}) for token {token} (remote: {remoteToken}) disconnected.");
                ((IConnection)sender).Dispose();
            };

            // remove the temporary handler
            connection.Disconnected -= HandleDisconnect;

            Diagnostic.Debug($"Unsolicited inbound transfer connection to {username} ({endpoint.Address}:{endpoint.Port}) for token {token} (remote: {remoteToken}) established.");
            SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.DirectTransfer, username, remoteToken), connection);
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IPeerConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Returns an existing, or gets a new connection using the details in the specified
        ///     <paramref name="connectToPeerResponse"/> and pierces the remote peer's firewall.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context, including the new or updated connection.</returns>
        public async Task<IMessageConnection> GetOrAddMessageConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            var key = new ConnectionKey(
                connectToPeerResponse.Username,
                connectToPeerResponse.IPAddress,
                connectToPeerResponse.Port);

            IMessageConnection connection = null;

            // get or add a connection. we only care about the semphore at this point, so discard the connection.
            var (semaphore, _) = await GetOrAddMessageConnectionRecordAsync(key.Username).ConfigureAwait(false);

            // await the semaphore we got back to ensure exclusive access over the code that follows. this is important because
            // while the GetOrAdd above either gets or retrieves a connection in a thread safe manner (through
            // ConcurrentDictionary), the connection itself is not synchronized.
            await semaphore.WaitAsync(MessageSemaphoreCancellationTokenSource.Token).ConfigureAwait(false);

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
                    connection = ConnectionFactory.GetMessageConnection(
                        connectToPeerResponse.Username,
                        connectToPeerResponse.IPAddress,
                        connectToPeerResponse.Port,
                        SoulseekClient.Options.PeerConnectionOptions);

                    connection.MessageRead += SoulseekClient.PeerMessageHandler.HandleMessage;
                    connection.Disconnected += MessageConnection_Disconnected;

                    try
                    {
                        await connection.ConnectAsync().ConfigureAwait(false);

                        var request = new PierceFirewall(connectToPeerResponse.Token).ToByteArray();
                        await connection.WriteAsync(request).ConfigureAwait(false);
                    }
                    catch
                    {
                        connection.Dispose();
                        throw;
                    }

                    (_, connection) = AddOrUpdateMessageConnectionRecord(connectToPeerResponse.Username, connection);

                    Diagnostic.Debug($"Solicited direct connection to {connectToPeerResponse.Username} ({connectToPeerResponse.IPAddress}:{connectToPeerResponse.Port}) established.");
                    return connection;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        ///     Gets a new or existing message connection to the specified <paramref name="username"/>.
        /// </summary>
        /// <remarks>
        ///     If a connection doesn't exist, a new direct connection is attempted first, and, if unsuccessful, an indirect
        ///     connection is attempted.
        /// </remarks>
        /// <param name="username">The username of the user to which to connect.</param>
        /// <param name="ipAddress">The remote IP address of the connection.</param>
        /// <param name="port">The remote port of the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the new or existing connection.</returns>
        public async Task<IMessageConnection> GetOrAddMessageConnectionAsync(string username, IPAddress ipAddress, int port, CancellationToken cancellationToken)
        {
            var (semaphore, _) = await GetOrAddMessageConnectionRecordAsync(username).ConfigureAwait(false);
            await semaphore.WaitAsync(MessageSemaphoreCancellationTokenSource.Token).ConfigureAwait(false);

            IMessageConnection connection;

            try
            {
                (_, connection) = await GetOrAddMessageConnectionRecordAsync(username).ConfigureAwait(false);

                if (connection != null)
                {
                    return connection;
                }
                else
                {
                    using (var directCts = new CancellationTokenSource())
                    using (var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token))
                    using (var indirectCts = new CancellationTokenSource())
                    using (var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token))
                    {
                        var direct = GetMessageConnectionOutboundDirectAsync(username, ipAddress, port, directLinkedCts.Token);
                        var indirect = GetMessageConnectionOutboundIndirectAsync(username, indirectLinkedCts.Token);

                        Diagnostic.Debug($"Attempting direct and indirect message connections to {username} ({ipAddress}:{port})");

                        var tasks = new[] { direct, indirect }.ToList();
                        Task<IMessageConnection> task;

                        do
                        {
                            task = await Task.WhenAny(tasks).ConfigureAwait(false);
                            tasks.Remove(task);
                        }
                        while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);

                        if (task.Status != TaskStatus.RanToCompletion)
                        {
                            throw new ConnectionException($"Failed to establish a message connection to {username} ({ipAddress}:{port})");
                        }

                        connection = await task.ConfigureAwait(false);
                        var isDirect = task == direct;

                        Diagnostic.Debug($"{connection.Context} message connection to {username} ({ipAddress}:{port}) established; cancelling {(isDirect ? "indirect" : "direct")} connection.");
                        (isDirect ? indirectCts : directCts).Cancel();

                        if (isDirect)
                        {
                            // if connecting directly, init the connection. for indirect connections the incoming peerinit is
                            // handled in the listener code to determine the connection type, so we don't need to handle it here.
                            var request = new PeerInit(SoulseekClient.Username, Constants.ConnectionType.Peer, SoulseekClient.GetNextToken()).ToByteArray();
                            await connection.WriteAsync(request, cancellationToken).ConfigureAwait(false);
                        }

                        (_, connection) = AddOrUpdateMessageConnectionRecord(username, connection);

                        Diagnostic.Debug($"Unsolicited {connection.Context} message connection to {username} ({ipAddress}:{port}) established.");
                        return connection;
                    }
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        ///     Gets a new transfer connection using the details in the specified <paramref name="connectToPeerResponse"/>,
        ///     pierces the remote peer's firewall, and retrieves the remote token.
        /// </summary>
        /// <param name="connectToPeerResponse">The response that solicited the connection.</param>
        /// <returns>The operation context, including the new connection and the associated remote token.</returns>
        public async Task<(IConnection Connection, int RemoteToken)> GetTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse)
        {
            var connection = ConnectionFactory.GetConnection(
                connectToPeerResponse.IPAddress,
                connectToPeerResponse.Port,
                SoulseekClient.Options.TransferConnectionOptions);

            connection.Context = Constants.ConnectionMethod.Direct;
            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Solicited direct transfer connection to {connectToPeerResponse.Username} ({connectToPeerResponse.IPAddress}:{connectToPeerResponse.Port}) for token {connectToPeerResponse.Token} disconnected.");

            int remoteToken;

            try
            {
                await connection.ConnectAsync().ConfigureAwait(false);

                var request = new PierceFirewall(connectToPeerResponse.Token);
                await connection.WriteAsync(request.ToByteArray()).ConfigureAwait(false);

                var remoteTokenBytes = await connection.ReadAsync(4).ConfigureAwait(false);
                remoteToken = BitConverter.ToInt32(remoteTokenBytes, 0);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            Diagnostic.Debug($"Solicited direct transfer connection to {connectToPeerResponse.Username} ({connectToPeerResponse.IPAddress}:{connectToPeerResponse.Port}) for token {connectToPeerResponse.Token} established.");
            return (connection, remoteToken);
        }

        /// <summary>
        ///     Gets a new transfer connection to the specified <paramref name="username"/> using the specified <paramref name="token"/>.
        /// </summary>
        /// <remarks>A direct connection is attempted first, and, if unsuccessful, an indirect connection is attempted.</remarks>
        /// <param name="username">The username of the user to which to connect.</param>
        /// <param name="ipAddress">The remote IP address of the connection.</param>
        /// <param name="port">The remote port of the connection.</param>
        /// <param name="token">The token with which to initialize the connection.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The operation context, including the new connection.</returns>
        public async Task<IConnection> GetTransferConnectionAsync(string username, IPAddress ipAddress, int port, int token, CancellationToken cancellationToken)
        {
            using (var directCts = new CancellationTokenSource())
            using (var directLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, directCts.Token))
            using (var indirectCts = new CancellationTokenSource())
            using (var indirectLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, indirectCts.Token))
            {
                var direct = GetTransferConnectionOutboundDirectAsync(ipAddress, port, token, directLinkedCts.Token);
                var indirect = GetTransferConnectionOutboundIndirectAsync(username, token, indirectLinkedCts.Token);

                Diagnostic.Debug($"Attempting direct and indirect transfer connections to {username} ({ipAddress}:{port})");

                var tasks = new[] { direct, indirect }.ToList();
                Task<IConnection> task;

                do
                {
                    task = await Task.WhenAny(tasks).ConfigureAwait(false);
                    tasks.Remove(task);
                }
                while (task.Status != TaskStatus.RanToCompletion && tasks.Count > 0);

                if (task.Status != TaskStatus.RanToCompletion)
                {
                    throw new ConnectionException($"Failed to establish a transfer connection to {username} ({ipAddress}:{port})");
                }

                var connection = await task.ConfigureAwait(false);
                var isDirect = task == direct;

                Diagnostic.Debug($"{connection.Context} transfer connection to {username} ({ipAddress}:{port}) established; cancelling {(isDirect ? "indirect" : "direct")} connection.");
                (isDirect ? indirectCts : directCts).Cancel();

                if (isDirect)
                {
                    // if connecting directly, init the connection. for indirect connections the incoming peerinit is handled in
                    // the listener code to determine the connection type, so we don't need to handle it here.
                    var request = new PeerInit(SoulseekClient.Username, Constants.ConnectionType.Transfer, token).ToByteArray();
                    await connection.WriteAsync(request, cancellationToken).ConfigureAwait(false);
                }

                // send our token (the remote token from the other side's perspective)
                await connection.WriteAsync(BitConverter.GetBytes(token), cancellationToken).ConfigureAwait(false);

                Diagnostic.Debug($"Unsolicited {connection.Context} transfer connection to {username} ({ipAddress}:{port}) established.");
                return connection;
            }
        }

        /// <summary>
        ///     Removes and disposes all active and queued connections.
        /// </summary>
        public void RemoveAndDisposeAll()
        {
            MessageSemaphoreCancellationTokenSource.Cancel();

            PendingSolicitationDictionary.Clear();

            while (!MessageConnectionDictionary.IsEmpty)
            {
                if (MessageConnectionDictionary.TryRemove(MessageConnectionDictionary.Keys.First(), out var value))
                {
                    value.Semaphore?.Dispose();
                    value.Connection?.Dispose();
                }
            }

            waitingMessageConnections = 0;
        }

        private (SemaphoreSlim Semaphore, IMessageConnection Connection) AddOrUpdateMessageConnectionRecord(string username, IMessageConnection connection)
        {
#pragma warning disable IDE0067 // Dispose objects before losing scope
            return MessageConnectionDictionary.AddOrUpdate(username, (new SemaphoreSlim(1, 1), connection), (k, v) =>
#pragma warning restore IDE0067 // Dispose objects before losing scope
            {
                // unassign the handler from the connection we are discarding to prevent it from removing a live connection.
                if (v.Connection != null)
                {
                    v.Connection.Disconnected -= MessageConnection_Disconnected;
                    v.Connection.Dispose();
                }

                return (v.Semaphore, connection);
            });
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

        private async Task<IMessageConnection> GetMessageConnectionOutboundDirectAsync(string username, IPAddress ipAddress, int port, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetMessageConnection(
                username,
                ipAddress,
                port,
                SoulseekClient.Options.PeerConnectionOptions);

            connection.Context = Constants.ConnectionMethod.Direct;
            connection.MessageRead += SoulseekClient.PeerMessageHandler.HandleMessage;
            connection.Disconnected += MessageConnection_Disconnected;

            try
            {
                await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            return connection;
        }

        private async Task<IMessageConnection> GetMessageConnectionOutboundIndirectAsync(string username, CancellationToken cancellationToken)
        {
            var solicitationToken = SoulseekClient.GetNextToken();

            try
            {
                PendingSolicitationDictionary.TryAdd(solicitationToken, username);

                await SoulseekClient.ServerConnection
                    .WriteAsync(new ConnectToPeerRequest(solicitationToken, username, Constants.ConnectionType.Peer).ToByteArray(), cancellationToken)
                    .ConfigureAwait(false);

                using (var incomingConnection = await SoulseekClient.Waiter
                    .Wait<IConnection>(new WaitKey(Constants.WaitKey.SolicitedPeerConnection, username, solicitationToken), null, cancellationToken)
                    .ConfigureAwait(false))
                {
                    var connection = ConnectionFactory.GetMessageConnection(
                        username,
                        incomingConnection.IPAddress,
                        incomingConnection.Port,
                        SoulseekClient.Options.PeerConnectionOptions,
                        incomingConnection.HandoffTcpClient());

                    connection.Context = Constants.ConnectionMethod.Indirect;
                    connection.MessageRead += SoulseekClient.PeerMessageHandler.HandleMessage;
                    connection.Disconnected += MessageConnection_Disconnected;

                    connection.StartReadingContinuously();

                    return connection;
                }
            }
            finally
            {
                PendingSolicitationDictionary.TryRemove(solicitationToken, out var _);
            }
        }

        private async Task<(SemaphoreSlim Semaphore, IMessageConnection Connection)> GetOrAddMessageConnectionRecordAsync(string username)
        {
            if (MessageConnectionDictionary.TryGetValue(username, out var record))
            {
                return record;
            }

            Interlocked.Increment(ref waitingMessageConnections);
            await MessageSemaphore.WaitAsync(MessageSemaphoreCancellationTokenSource.Token).ConfigureAwait(false);
            Interlocked.Decrement(ref waitingMessageConnections);

            record = MessageConnectionDictionary.GetOrAdd(username, (k) => (new SemaphoreSlim(1, 1), null));

            if (record.Connection == null)
            {
                Diagnostic.Debug($"Initialized message connection to {username}");
            }

            return record;
        }

        private async Task<IConnection> GetTransferConnectionOutboundDirectAsync(IPAddress ipAddress, int port, int token, CancellationToken cancellationToken)
        {
            var connection = ConnectionFactory.GetConnection(ipAddress, port, SoulseekClient.Options.TransferConnectionOptions);

            connection.Context = Constants.ConnectionMethod.Direct;
            connection.Disconnected += (sender, e) => Diagnostic.Debug($"Outbound direct transfer connection for token {token} ({ipAddress}:{port}) disconnected.");

            try
            {
                await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                connection.Dispose();
                throw;
            }

            return connection;
        }

        private async Task<IConnection> GetTransferConnectionOutboundIndirectAsync(string username, int token, CancellationToken cancellationToken)
        {
            var solicitationToken = SoulseekClient.GetNextToken();

            try
            {
                PendingSolicitationDictionary.TryAdd(solicitationToken, username);

                await SoulseekClient.ServerConnection
                    .WriteAsync(new ConnectToPeerRequest(solicitationToken, username, Constants.ConnectionType.Transfer).ToByteArray(), cancellationToken)
                    .ConfigureAwait(false);

                using (var incomingConnection = await SoulseekClient.Waiter
                    .Wait<IConnection>(new WaitKey(Constants.WaitKey.SolicitedPeerConnection, username, solicitationToken), null, cancellationToken)
                    .ConfigureAwait(false))
                {
                    var connection = ConnectionFactory.GetConnection(
                        incomingConnection.IPAddress,
                        incomingConnection.Port,
                        SoulseekClient.Options.TransferConnectionOptions,
                        incomingConnection.HandoffTcpClient());

                    connection.Context = Constants.ConnectionMethod.Indirect;
                    connection.Disconnected += (sender, e) => Diagnostic.Debug($"Outbound indirect transfer connection for token {token} ({incomingConnection.IPAddress}:{incomingConnection.Port}) disconnected.");

                    return connection;
                }
            }
            finally
            {
                PendingSolicitationDictionary.TryRemove(solicitationToken, out var _);
            }
        }

        private void MessageConnection_Disconnected(object sender, string message)
        {
            var connection = (IMessageConnection)sender;
            RemoveMessageConnectionRecord(connection);
            connection.Dispose();
        }

        private void RemoveMessageConnectionRecord(IMessageConnection connection)
        {
            if (MessageConnectionDictionary.TryRemove(connection.Key.Username, out _))
            {
                Diagnostic.Debug($"Removing message connection to {connection.Key.Username} ({connection.IPAddress}:{connection.Port})");

                // only release if we successfully removed a connection. this can throw if another thread released it first and
                // the semaphore tries to release more than its capacity.
                MessageSemaphore.Release();
            }
        }
    }
}