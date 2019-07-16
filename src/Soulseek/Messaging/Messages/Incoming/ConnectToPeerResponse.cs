// <copyright file="ConnectToPeerResponse.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using System;
    using System.Net;
    using Soulseek.Exceptions;

    /// <summary>
    ///     A server response which solicits a peer connection.
    /// </summary>
    internal sealed class ConnectToPeerResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectToPeerResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the peer.</param>
        /// <param name="type">The connection type ('P' for message or 'F' for transfer).</param>
        /// <param name="ipAddress">The IP address to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <param name="token">The unique connection token.</param>
        public ConnectToPeerResponse(string username, string type, IPAddress ipAddress, int port, int token)
        {
            Username = username;
            Type = type;
            IPAddress = ipAddress;
            Port = port;
            Token = token;
        }

        /// <summary>
        ///     Gets the IP address to which to connect.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the port to which to connect.
        /// </summary>
        public int Port { get; }

        /// <summary>
        ///     Gets the unique connection token.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the connection type ('P' for message or 'F' for transfer).
        /// </summary>
        public string Type { get; }

        /// <summary>
        ///     Gets the username of the peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="ConnectToPeerResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static ConnectToPeerResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.ConnectToPeer)
            {
                throw new MessageException($"Message Code mismatch creating Connect To Peer response (expected: {(int)MessageCode.Server.ConnectToPeer}, received: {(int)code}");
            }

            var username = reader.ReadString();
            var type = reader.ReadString();

            var ipBytes = reader.ReadBytes(4);
            Array.Reverse(ipBytes);
            var ipAddress = new IPAddress(ipBytes);

            var port = reader.ReadInteger();
            var token = reader.ReadInteger();

            return new ConnectToPeerResponse(username, type, ipAddress, port, token);
        }
    }
}