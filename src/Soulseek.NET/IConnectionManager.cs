// <copyright file="IConnectionManager.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     Manages peer <see cref="IConnection"/> instances for the application.
    /// </summary>
    internal interface IConnectionManager : IDisposable
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
        Task AddAsync(IMessageConnection connection);

        /// <summary>
        ///     Gets a <see cref="MessageConnection"/> instance.
        /// </summary>
        /// <param name="type">The connection type (Peer, Server)</param>
        /// <param name="username">The username of the peer associated with the connection, if applicable.</param>
        /// <param name="ipAddress">The remote IP address of the connection.</param>
        /// <param name="port">The remote port of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <returns>The created Connection.</returns>
        IMessageConnection GetMessageConnection(MessageConnectionType type, string username, IPAddress ipAddress, int port, ConnectionOptions options = null);

        IMessageConnection GetServerMessageConnection(string address, int port, ConnectionOptions options);

        Task<IMessageConnection> GetSolicitedPeerConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options, CancellationToken cancellationToken);

        Task<IConnection> GetTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options, CancellationToken cancellationToken);

        Task<IMessageConnection> GetUnsolicitedPeerConnectionAsync(string localUsername, string remoteUsername, ConnectionOptions options, CancellationToken cancellationToken);

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
        Task RemoveAsync(IMessageConnection connection);
    }
}