// <copyright file="ConnectionManagerOfT.cs" company="JP Dillingham">
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
    using System.Threading.Tasks;

    internal class ConnectionManager<T>
        where T : IConnection
    {
        internal ConnectionManager(int concurrentConnections)
        {
            ConcurrentConnections = concurrentConnections;
        }

        internal int Active => Connections.Count;
        internal int Queued => ConnectionQueue.Count;
        private int ConcurrentConnections { get; set; }
        private ConcurrentQueue<T> ConnectionQueue { get; set; } = new ConcurrentQueue<T>();
        private ConcurrentDictionary<ConnectionKey, T> Connections { get; set; } = new ConcurrentDictionary<ConnectionKey, T>();

        internal async Task Add(T connection)
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

        internal async Task Remove(T connection)
        {
            var key = connection.Key;

            connection.Dispose();
            Connections.TryRemove(key, out var _);

            if (Connections.Count < ConcurrentConnections &&
                ConnectionQueue.TryDequeue(out var nextConnection))
            {
                if (Connections.TryAdd(nextConnection.Key, nextConnection))
                {
                    await TryConnectAsync(nextConnection);
                }
            }
        }

        private async Task TryConnectAsync(T connection)
        {
            try
            {
                await connection.ConnectAsync();
            }
            catch (Exception)
            {
            }
        }
    }
}