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

namespace Soulseek.NET.Tcp
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    internal class ConnectionManager<T> : IConnectionManager<T>, IDisposable
        where T : IConnection
    {
        #region Internal Constructors

        internal ConnectionManager(int concurrentConnections)
        {
            ConcurrentConnections = concurrentConnections;
        }

        #endregion Internal Constructors

        #region Internal Properties

        public int Active => Connections.Count;
        public int Queued => ConnectionQueue.Count;

        #endregion Internal Properties

        #region Private Properties

        private int ConcurrentConnections { get; set; }
        private ConcurrentQueue<T> ConnectionQueue { get; set; } = new ConcurrentQueue<T>();
        private ConcurrentDictionary<ConnectionKey, T> Connections { get; set; } = new ConcurrentDictionary<ConnectionKey, T>();

        private bool Disposed { get; set; }

        #endregion Private Properties

        #region Public Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion Public Methods

        #region Internal Methods

        public async Task Add(T connection)
        {
            if (Connections.Count < ConcurrentConnections)
            {
                if (Connections.TryAdd(connection.Key, connection))
                {
                    await TryConnectAsync(connection);
                }
            }
            else
            {
                ConnectionQueue.Enqueue(connection);
            }
        }

        public T Get(ConnectionKey key)
        {
            var queuedConnection = ConnectionQueue.FirstOrDefault(c => c.Key.Equals(key));

            if (!EqualityComparer<T>.Default.Equals(queuedConnection, default(T)))
            {
                return queuedConnection;
            }
            else if (Connections.ContainsKey(key))
            {
                return Connections[key];
            }

            return default(T);
        }

        public async Task Remove(T connection)
        {
            var key = connection?.Key;

            connection?.Dispose();
            Connections.TryRemove(key, out var _);

            if (Connections.Count < ConcurrentConnections &&
                ConnectionQueue.TryDequeue(out var nextConnection))
            {
                if (!Connections.ContainsKey(nextConnection.Key) && Connections.TryAdd(nextConnection.Key, nextConnection))
                {
                    await TryConnectAsync(nextConnection);
                }
            }
        }

        public void RemoveAll()
        {
            ConnectionQueue.DequeueAndDisposeAll();
            Connections.RemoveAndDisposeAll();
        }

        #endregion Internal Methods

        #region Protected Methods

        protected virtual void Dispose(bool disposing)
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

        #endregion Protected Methods

        #region Private Methods

        private async Task TryConnectAsync(T connection)
        {
            try
            {
                await connection?.ConnectAsync();
            }
            catch (Exception)
            {
            }
        }

        #endregion Private Methods
    }
}