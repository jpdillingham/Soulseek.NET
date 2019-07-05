// <copyright file="PeerInitResponse.cs" company="JP Dillingham">
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
    using System.Linq;
    using System.Text;

    /// <summary>
    ///     Initiates a peer connection.
    /// </summary>
    public sealed class PeerInitResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerInitResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the peer.</param>
        /// <param name="transferType">The transfer type (P or F).</param>
        /// <param name="token">The unique token for the connection.</param>
        internal PeerInitResponse(string username, string transferType, int token)
        {
            Username = username;
            TransferType = transferType;
            Token = token;
        }

        /// <summary>
        ///     Gets tue username of the peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Gets the transfer type (P or F).
        /// </summary>
        public string TransferType { get; }

        /// <summary>
        ///     Gets the unique token for the connection.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Parses a new instance of <see cref="PeerInitResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The message from which to parse.</param>
        /// <param name="response">The parsed instance.</param>
        /// <returns>A value indicating whether the message was successfully parsed.</returns>
        public static bool TryParse(byte[] bytes, out PeerInitResponse response)
        {
            response = null;

            try
            {
                var code = (InitializationCode)bytes.Skip(4).ToArray()[0];

                if (code != InitializationCode.PeerInit)
                {
                    return false;
                }

                var restBytes = bytes.Skip(5).ToArray();
                var nameLen = BitConverter.ToInt32(restBytes, 0);
                var username = Encoding.ASCII.GetString(restBytes.Skip(4).Take(nameLen).ToArray());
                var typeLen = BitConverter.ToInt32(restBytes, 4 + nameLen);
                var transferType = Encoding.ASCII.GetString(restBytes.Skip(4 + nameLen + 4).Take(typeLen).ToArray());
                var token = BitConverter.ToInt32(restBytes, 4 + nameLen + 4 + typeLen);

                response = new PeerInitResponse(username, transferType, token);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
