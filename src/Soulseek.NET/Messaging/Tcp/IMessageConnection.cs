// <copyright file="IMessageConnection.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Messaging.Tcp
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     Provides client connections to the Soulseek network.
    /// </summary>
    internal interface IMessageConnection : IConnection
    {
        /// <summary>
        ///     Occurs when a new message is received.
        /// </summary>
        event EventHandler<Message> MessageRead;

        /// <summary>
        ///     Gets the username of the peer associated with the connection, if applicable.
        /// </summary>
        string Username { get; }

        /// <summary>
        ///     Gets the connection type (Peer, Server).
        /// </summary>
        MessageConnectionType Type { get; }

        /// <summary>
        ///     Asynchronously writes the specified message to the connection.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task WriteMessageAsync(Message message, CancellationToken cancellationToken);
    }
}