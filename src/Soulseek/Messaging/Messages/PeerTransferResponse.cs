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
        /// <param name="message">The reason the transfer was disallowed.</param>
        internal PeerTransferResponse(int token, string message)
        {
            Token = token;
            Allowed = false;
            Message = message;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerTransferResponse"/> class.
        /// </summary>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="fileSize">The size of the file being transferred.</param>
        internal PeerTransferResponse(int token, long fileSize)
        {
            Token = token;
            Allowed = true;
            FileSize = fileSize;
        }

        internal PeerTransferResponse(int token)
        {
            Token = token;
            Allowed = true;
        }

        /// <summary>
        ///     Gets a value indicating whether the transfer is allowed.
        /// </summary>
        public bool Allowed { get; }

        /// <summary>
        ///     Gets the size of the file being transferred, if allowed.
        /// </summary>
        public long FileSize { get; }

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

            if (allowed && reader.HasMoreData)
            {
                var fileSize = reader.ReadLong();
                return new PeerTransferResponse(token, fileSize);
            }
            else if (!allowed)
            {
                var msg = reader.ReadString();
                return new PeerTransferResponse(token, msg);
            }

            return new PeerTransferResponse(token);
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
                builder.WriteLong(FileSize);
            }
            else
            {
                builder.WriteString(Message);
            }

            return builder.Build();
        }
    }
}