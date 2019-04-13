// <copyright file="ConnectionManager.cs" company="JP Dillingham">
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
    ///     Manages a queue of <see cref="IConnection"/>
    /// </summary>
    internal sealed class ConnectionManager : IConnectionManager
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionManager"/> class.
        /// </summary>
        /// <param name="concurrentConnections">The number of allowed concurrent connections.</param>
        internal ConnectionManager(IMessageConnection serverConnection, IWaiter waiter, int concurrentConnections = 500)
        {
            ServerConnection = serverConnection;
            Waiter = waiter;
            ConcurrentConnections = concurrentConnections;
        }

        /// <summary>
        ///     Gets the number of active connections.
        /// </summary>
        public int Active => Connections.Count;

        /// <summary>
        ///     Gets the number of allowed concurrent connections.
        /// </summary>
        public int ConcurrentConnections { get; private set; }

        /// <summary>
        ///     Gets the number of queued connections.
        /// </summary>
        public int Queued => ConnectionQueue.Count;

        private ConcurrentQueue<IMessageConnection> ConnectionQueue { get; } = new ConcurrentQueue<IMessageConnection>();
        private ConcurrentDictionary<ConnectionKey, IMessageConnection> Connections { get; } = new ConcurrentDictionary<ConnectionKey, IMessageConnection>();
        private bool Disposed { get; set; }
        private IMessageConnection ServerConnection { get; set; }
        private IWaiter Waiter { get; set; }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IConnectionManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<IMessageConnection> GetSolicitedConnectionAsync(ConnectToPeerResponse connectToPeerResponse, EventHandler<Message> messageHandler, ConnectionOptions options, CancellationToken cancellationToken)
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
                await conn.WriteAsync(request.ToByteArray(), cancellationToken).ConfigureAwait(false);
            };

            connection.Disconnected += async (sender, e) => await RemoveAsync((IMessageConnection)sender).ConfigureAwait(false);

            connection.MessageRead += messageHandler;

            await AddAsync(connection).ConfigureAwait(false);
            return connection;
        }

        public async Task<IConnection> GetTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options, CancellationToken cancellationToken)
        {
            var connection = new Connection(connectToPeerResponse.IPAddress, connectToPeerResponse.Port, options);
            await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

            var request = new PierceFirewallRequest(connectToPeerResponse.Token);
            await connection.WriteAsync(request.ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);

            return connection;
        }

        private ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)> MessageConnections = new ConcurrentDictionary<ConnectionKey, (SemaphoreSlim Semaphore, IMessageConnection Connection)>();

        private async Task<(SemaphoreSlim Semaphore, IMessageConnection Connection)> GetOrAddMessageConnection(ConnectionKey key)
        {
            // todo: implement queuing logic; use a lock or something to block threads that would be adding new connections
            return MessageConnections.GetOrAdd(key, (new SemaphoreSlim(1, 1), null));
        }

        private void AddOrUpdateMessageConnection(ConnectionKey key, IMessageConnection connection)
        {
            MessageConnections.AddOrUpdate(key, (new SemaphoreSlim(1, 1), connection), (k, v) => (v.Semaphore, connection));
        }

        private void RemoveMessageConnection(IMessageConnection connection)
        {
            MessageConnections.TryRemove(connection.Key, out _);
        }

        public async Task<IMessageConnection> GetUnsolicitedConnectionAsync(string localUsername, string remoteUsername, EventHandler<Message> messageHandler, ConnectionOptions options, CancellationToken cancellationToken)
        {
            var key = await GetPeerConnectionKeyAsync(remoteUsername, cancellationToken).ConfigureAwait(false);
            var (semaphore, connection) = await GetOrAddMessageConnection(key).ConfigureAwait(false);

            await semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (connection != null && connection.State == ConnectionState.Connected)
                {
                    return connection;
                }
                else
                {
                    connection = new MessageConnection(MessageConnectionType.Peer, key.Username, key.IPAddress, key.Port, options);
                    connection.MessageRead += messageHandler;
                    connection.Disconnected += (sender, e) => RemoveMessageConnection(connection);

                    await connection.ConnectAsync(cancellationToken).ConfigureAwait(false);

                    var token = new Random().Next(1, 2147483647);
                    await connection.WriteAsync(new PeerInitRequest(localUsername, "P", token).ToMessage().ToByteArray(), cancellationToken).ConfigureAwait(false);

                    AddOrUpdateMessageConnection(key, connection);
                }
            }
            finally
            {
                semaphore.Release();
            }

            return connection;
        }

        /// <summary>
        ///     Disposes and removes all active and queued connections.
        /// </summary>
        public void RemoveAndDisposeAll()
        {
            ConnectionQueue.DequeueAndDisposeAll();
            Connections.RemoveAndDisposeAll();
        }

        private async Task AddAsync(IMessageConnection connection)
        {
            if (connection == null || connection.Key == null)
            {
                return;
            }

            if (Connections.Count < ConcurrentConnections)
            {
                if (Connections.TryAdd(connection.Key, connection))
                {
                    await TryConnectAsync(connection).ConfigureAwait(false);
                }
            }
            else
            {
                ConnectionQueue.Enqueue(connection);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    RemoveAndDisposeAll();
                }

                Disposed = true;
            }
        }

        private IMessageConnection Get(ConnectionKey connectionKey)
        {
            if (connectionKey != null)
            {
                var queuedConnection = ConnectionQueue.FirstOrDefault(c => c.Key.Equals(connectionKey));

                if (!EqualityComparer<IMessageConnection>.Default.Equals(queuedConnection, default(IMessageConnection)))
                {
                    return queuedConnection;
                }
                else if (Connections.ContainsKey(connectionKey))
                {
                    return Connections[connectionKey];
                }
            }

            return default(IMessageConnection);
        }

        // todo: move this back to SoulseekClient
        private async Task<ConnectionKey> GetPeerConnectionKeyAsync(string username, CancellationToken cancellationToken)
        {
            var addressWait = Waiter.Wait<GetPeerAddressResponse>(new WaitKey(MessageCode.ServerGetPeerAddress, username), cancellationToken: cancellationToken);

            var request = new GetPeerAddressRequest(username);
            await ServerConnection.WriteMessageAsync(request.ToMessage(), cancellationToken).ConfigureAwait(false);

            var address = await addressWait.ConfigureAwait(false);
            return new ConnectionKey(username, address.IPAddress, address.Port, MessageConnectionType.Peer);
        }

        private async Task RemoveAsync(IMessageConnection connection)
        {
            if (connection == null || connection.Key == null)
            {
                return;
            }

            if (Connections.TryRemove(connection.Key, out var _))
            {
                connection.Dispose();
            }
            else
            {
                return;
            }

            if (Connections.Count < ConcurrentConnections &&
                ConnectionQueue.TryDequeue(out var nextConnection) &&
                Connections.TryAdd(nextConnection.Key, nextConnection))
            {
                await TryConnectAsync(nextConnection).ConfigureAwait(false);
            }
        }

        private async Task TryConnectAsync(IMessageConnection connection)
        {
            try
            {
                await connection.ConnectAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await RemoveAsync(connection).ConfigureAwait(false);
            }
        }
    }
}