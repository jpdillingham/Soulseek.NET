// <copyright file="IConnectionManager{T}.cs" company="JP Dillingham">
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
    using System.Threading.Tasks;

    /// <summary>
    ///     Manages a queue of <see cref="IConnection"/>
    /// </summary>
    /// <typeparam name="T">The Type of the managed connection implementation.</typeparam>
    internal interface IConnectionManager<T> : IDisposable
        where T : IConnection
    {
        /// <summary>
        ///     Gets the number of active connections.
        /// </summary>
        int Active { get; }

        /// <summary>
        ///     Gets the number of allowed concurrent connections.
        /// </summary>
        int ConcurrentConnections { get; }

        /// <summary>
        ///     Gets the number of queued connections.
        /// </summary>
        int Queued { get; }

        /// <summary>
        ///     Asynchronously adds the specified <paramref name="connection"/> to the manager.
        /// </summary>
        /// <remarks>
        ///     If <see cref="Active"/> is fewer than <see cref="ConcurrentConnections"/>, the connection is connected immediately.
        ///     Otherwise, it is queued.
        /// </remarks>
        /// <param name="connection">The connection to add.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task AddAsync(T connection);

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IConnectionManager{T}"/>.
        /// </summary>
        new void Dispose();

        /// <summary>
        ///     Returns the connection matching the specified <paramref name="connectionKey"/>
        /// </summary>
        /// <param name="connectionKey">The unique identifier of the connection to retrieve.</param>
        /// <returns>The connection matching the specified connection key.</returns>
        T Get(ConnectionKey connectionKey);

        /// <summary>
        ///     Disposes and removes all active and queued connections.
        /// </summary>
        void RemoveAll();

        /// <summary>
        ///     Asynchronously disposes and removes the specified <paramref name="connection"/> from the manager.
        /// </summary>
        /// <remarks>If <see cref="Queued"/> is greater than zero, the next connection is removed from the queue and connected.</remarks>
        /// <param name="connection">The connection to remove.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task RemoveAsync(T connection);
    }
}