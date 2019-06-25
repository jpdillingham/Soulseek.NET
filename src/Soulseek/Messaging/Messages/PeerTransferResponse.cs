// <copyright file="PeerTransferResponse.cs" company="JP Dillingham">
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
    using Soulseek.Exceptions;

    /// <summary>
    ///     An incoming response to a peer transfer request.
    /// </summary>
    internal sealed class PeerTransferResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerTransferResponse"/> class.
        /// </summary>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="allowed">A value indicating whether the transfer is allowed.</param>
        /// <param name="message">The reason the transfer was disallowed, if applicable.</param>
        internal PeerTransferResponse(int token, bool allowed, string message)
        {
            Token = token;
            Allowed = allowed;
            Message = message;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerTransferResponse"/> class.
        /// </summary>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="allowed">A value indicating whether the transfer is allowed.</param>
        /// <param name="fileSize">The size of the file being transferred, if allowed.</param>
        internal PeerTransferResponse(int token, bool allowed, int fileSize)
        {
            Token = token;
            Allowed = allowed;
            FileSize = fileSize;
        }

        /// <summary>
        ///     Gets a value indicating whether the transfer is allowed.
        /// </summary>
        public bool Allowed { get; }

        /// <summary>
        ///     Gets the size of the file being transferred, if allowed.
        /// </summary>
        public int FileSize { get; }

        /// <summary>
        ///     Gets the reason the transfer was disallowed, if applicable.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the unique token for the transfer.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Parses a new instance of <see cref="PeerTransferResponse"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PeerTransferResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerTransferResponse)
            {
                throw new MessageException($"Message Code mismatch creating Peer Transfer Response (expected: {(int)MessageCode.PeerTransferResponse}, received: {(int)reader.Code}.");
            }

            var token = reader.ReadInteger();
            var allowed = reader.ReadByte() == 1;

            int fileSize = default(int);
            string msg = default(string);

            if (allowed)
            {
                fileSize = reader.ReadInteger();
                return new PeerTransferResponse(token, allowed, fileSize);
            }
            else
            {
                msg = reader.ReadString();
                return new PeerTransferResponse(token, allowed, msg);
            }

        }

        /// <summary>
        ///     Constructs a <see cref="Message"/> from this request.
        /// </summary>
        /// <returns>The constructed message.</returns>
        public Message ToMessage()
        {
            var builder = new MessageBuilder()
                .Code(MessageCode.PeerTransferResponse)
                .WriteInteger(Token)
                .WriteByte((byte)(Allowed ? 1 : 0));

            if (Allowed)
            {
                builder.WriteInteger(FileSize);
            }
            else
            {
                builder.WriteString(Message);
            }

            return builder.Build();
        }
    }
}