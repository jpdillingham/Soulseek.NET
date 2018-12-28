// <copyright file="PeerInitRequest.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Messaging.Requests
{
    /// <summary>
    ///     Initializes a peer connection.
    /// </summary>
    public class PeerInitRequest
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerInitRequest"/> class.
        /// </summary>
        /// <param name="username">The username of the peer.</param>
        /// <param name="transferType">The transfer type (P or F)</param>
        /// <param name="token">The unique token for the connection.</param>
        public PeerInitRequest(string username, string transferType, int token)
        {
            Username = username;
            TransferType = transferType;
            Token = token;
        }

        /// <summary>
        ///     Gets the unique token for the connection.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the transfer type (P or F)
        /// </summary>
        public string TransferType { get; }

        /// <summary>
        ///     Gets tue username of the peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Constructs a <see cref="Message"/> from this request.
        /// </summary>
        /// <returns>The constructed message.</returns>
        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(0x1)
                .WriteString(Username)
                .WriteString(TransferType)
                .WriteInteger(Token)
                .Build();
        }
    }
}