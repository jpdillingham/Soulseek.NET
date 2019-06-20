// <copyright file="IConnectionFactory.cs" company="JP Dillingham">
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
    using System.Net;
    using Soulseek.Messaging.Tcp;
    using Soulseek.Tcp;

    /// <summary>
    ///     Creates connections.
    /// </summary>
    internal interface IConnectionFactory
    {
        /// <summary>
        ///     Gets a <see cref="IConnection"/> with the specified parameters.
        /// </summary>
        /// <param name="ipAddress">The remote IP address of the connection.</param>
        /// <param name="port">The remote port of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        /// <returns>The created connection.</returns>
        IConnection GetConnection(IPAddress ipAddress, int port, ConnectionOptions options = null, ITcpClient tcpClient = null);

        /// <summary>
        ///     Gets a <see cref="IMessageConnection"/> with the specified parameters.
        /// </summary>
        /// <param name="type">The connection type (Peer, Server).</param>
        /// <param name="username">The username of the peer associated with the connection, if applicable.</param>
        /// <param name="ipAddress">The remote IP address of the connection.</param>
        /// <param name="port">The remote port of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        /// <returns>The created connection.</returns>
        IMessageConnection GetMessageConnection(MessageConnectionType type, string username, IPAddress ipAddress, int port, ConnectionOptions options = null, ITcpClient tcpClient = null);
    }
}
