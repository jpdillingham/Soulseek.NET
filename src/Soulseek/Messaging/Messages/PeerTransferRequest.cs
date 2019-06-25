// <copyright file="PeerTransferRequest.cs" company="JP Dillingham">
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
    ///     An incoming request to transfer a file.
    /// </summary>
    internal sealed class PeerTransferRequest
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerTransferRequest"/> class.
        /// </summary>
        /// <param name="direction">The direction of the transfer (download, upload).</param>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="filename">The name of the file being transferred.</param>
        /// <param name="fileSize">The size of the file being transferred.</param>
        internal PeerTransferRequest(TransferDirection direction, int token, string filename, long fileSize = 0)
        {
            Direction = direction;
            Token = token;
            Filename = filename;
            FileSize = fileSize;
        }

        /// <summary>
        ///     Gets the direction of the transfer (download, upload).
        /// </summary>
        public TransferDirection Direction { get; }

        /// <summary>
        ///     Gets the name of the file being transferred.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the size of the file being transferred.
        /// </summary>
        public long FileSize { get; }

        /// <summary>
        ///     Gets the unique token for the transfer.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Parses a new instance of <see cref="PeerTransferRequest"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PeerTransferRequest Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerTransferRequest)
            {
                throw new MessageException($"Message Code mismatch creating Peer Transfer Request response (expected: {(int)MessageCode.PeerTransferRequest}, received: {(int)reader.Code}.");
            }

            var direction = (TransferDirection)reader.ReadInteger();
            var token = reader.ReadInteger();
            var filename = reader.ReadString();
            var fileSize = reader.ReadLong();

            return new PeerTransferRequest(direction, token, filename, fileSize);
        }

        /// <summary>
        ///     Constructs a <see cref="Message"/> from this request.
        /// </summary>
        /// <returns>The constructed message.</returns>
        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.PeerTransferRequest)
                .WriteInteger((int)Direction)
                .WriteInteger(Token)
                .WriteString(Filename)
                .WriteLong(FileSize)
                .Build();
        }
    }
}