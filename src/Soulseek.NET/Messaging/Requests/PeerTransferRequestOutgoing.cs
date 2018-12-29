// <copyright file="PeerTransferRequestOutgoing.cs" company="JP Dillingham">
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
    ///     Requests a transfer from a peer.
    /// </summary>
    public class PeerTransferRequestOutgoing
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerTransferRequestOutgoing"/> class.
        /// </summary>
        /// <param name="direction">The direction of the transfer (upload or download).</param>
        /// <param name="token">The unique transfer token.</param>
        /// <param name="filename">The filename of the file to transfer.</param>
        /// <param name="fileSize">The file size in bytes, if uploading.</param>
        public PeerTransferRequestOutgoing(TransferDirection direction, int token, string filename, int fileSize = 0)
        {
            Direction = direction;
            Token = token;
            Filename = filename;
            FileSize = fileSize;
        }

        /// <summary>
        ///     Gets the direction of the transfer.
        /// </summary>
        public TransferDirection Direction { get; }

        /// <summary>
        ///     Gets the filename of the file to transfer.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the file size in bytes, if uploading.
        /// </summary>
        public int FileSize { get; }

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
                .Code(MessageCode.PeerTransferRequest)
                .WriteInteger((int)Direction)
                .WriteInteger(Token)
                .WriteString(Filename)
                .WriteInteger(FileSize)
                .Build();
        }
    }
}