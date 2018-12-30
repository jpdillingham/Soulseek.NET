// <copyright file="PeerTransferResponseOutgoing.cs" company="JP Dillingham">
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
    ///     Responds to a transfer request from a peer.
    /// </summary>
    public class PeerTransferResponseOutgoing
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerTransferResponseOutgoing"/> class.
        /// </summary>
        /// <param name="token">The unique transfer token.</param>
        /// <param name="allowed">A value indicating whether the transfer is allowed to begin.</param>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="message">The reason the transfer is disallowed, if applicable.</param>
        public PeerTransferResponseOutgoing(int token, bool allowed, int fileSize, string message)
        {
            Token = token;
            Allowed = allowed;
            FileSize = fileSize;
            Message = message;
        }

        /// <summary>
        ///     Gets a value indicating whether the transfer is allowed to begin.
        /// </summary>
        public bool Allowed { get; }

        /// <summary>
        ///     Gets the file size in bytes.
        /// </summary>
        public int FileSize { get; }

        /// <summary>
        ///     Gets the reason the transfer is disallowed, if applicable.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the unique transfer token.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Constructs a <see cref="Message"/> from this request.
        /// </summary>
        /// <returns>The constructed message.</returns>
        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.PeerTransferResponse)
                .WriteInteger(Token)
                .WriteByte((byte)(Allowed ? 1 : 0))
                .WriteInteger(FileSize)
                .WriteString(Message)
                .Build();
        }
    }
}