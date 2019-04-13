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
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
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

        Task<IMessageConnection> GetSolicitedConnectionAsync(ConnectToPeerResponse connectToPeerResponse, EventHandler<Message> messageHandler, ConnectionOptions options, CancellationToken cancellationToken);

        Task<IConnection> GetTransferConnectionAsync(ConnectToPeerResponse connectToPeerResponse, ConnectionOptions options, CancellationToken cancellationToken);

        Task<IMessageConnection> GetUnsolicitedConnectionAsync(string localUsername, string remoteUsername, EventHandler<Message> messageHandler, ConnectionOptions options, CancellationToken cancellationToken);

        /// <summary>
        ///     Disposes and removes all active and queued connections.
        /// </summary>
        void RemoveAll();
    }
}