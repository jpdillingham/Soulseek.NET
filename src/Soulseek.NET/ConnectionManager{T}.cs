// <copyright file="ConnectionManager{T}.cs" company="JP Dillingham">
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
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     Manages a queue of <see cref="IConnection"/>
    /// </summary>
    /// <typeparam name="T">The Type of the managed connection implementation.</typeparam>
    internal sealed class ConnectionManager<T> : IConnectionManager<T>
        where T : IConnection
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionManager{T}"/> class.
        /// </summary>
        /// <param name="concurrentConnections">The number of allowed concurrent connections.</param>
        internal ConnectionManager(int concurrentConnections = 500)
        {
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

        private ConcurrentQueue<T> ConnectionQueue { get; } = new ConcurrentQueue<T>();
        private ConcurrentDictionary<ConnectionKey, T> Connections { get; } = new ConcurrentDictionary<ConnectionKey, T>();
        private bool Disposed { get; set; }

        /// <summary>
        ///     Asynchronously adds the specified <paramref name="connection"/> to the manager.
        /// </summary>
        /// <remarks>
        ///     If <see cref="Active"/> is fewer than <see cref="ConcurrentConnections"/>, the connection is connected immediately.  Otherwise, it is queued.
        /// </remarks>
        /// <param name="connection">The connection to add.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task AddAsync(T connection)
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

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IConnectionManager{T}"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Returns the connection matching the specified <paramref name="connectionKey"/>
        /// </summary>
        /// <param name="connectionKey">The unique identifier of the connection to retrieve.</param>
        /// <returns>The connection matching the specified connection key.</returns>
        public T Get(ConnectionKey connectionKey)
        {
            if (connectionKey != null)
            {
                var queuedConnection = ConnectionQueue.FirstOrDefault(c => c.Key.Equals(connectionKey));

                if (!EqualityComparer<T>.Default.Equals(queuedConnection, default(T)))
                {
                    return queuedConnection;
                }
                else if (Connections.ContainsKey(connectionKey))
                {
                    return Connections[connectionKey];
                }
            }

            return default(T);
        }

        /// <summary>
        ///     Disposes and removes all active and queued connections.
        /// </summary>
        public void RemoveAll()
        {
            ConnectionQueue.DequeueAndDisposeAll();
            Connections.RemoveAndDisposeAll();
        }

        /// <summary>
        ///     Asynchronously disposes and removes the specified <paramref name="connection"/> from the manager.
        /// </summary>
        /// <remarks>
        ///     If <see cref="Queued"/> is greater than zero, the next connection is removed from the queue and connected.
        /// </remarks>
        /// <param name="connection">The connection to remove.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task RemoveAsync(T connection)
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

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    RemoveAll();
                }

                Disposed = true;
            }
        }

        private async Task TryConnectAsync(T connection)
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