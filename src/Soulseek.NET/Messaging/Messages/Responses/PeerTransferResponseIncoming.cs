// <copyright file="PeerTransferResponseIncoming.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Messaging.Messages
{
    using Soulseek.NET.Exceptions;

    /// <summary>
    ///     An incoming response to a peer transfer request.
    /// </summary>
    internal sealed class PeerTransferResponseIncoming
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerTransferResponseIncoming"/> class.
        /// </summary>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="allowed">A value indicating whether the transfer is allowed.</param>
        /// <param name="fileSize">The size of the file being transferred, if allowed.</param>
        /// <param name="message">The reason the transfer was disallowed, if applicable.</param>
        internal PeerTransferResponseIncoming(int token, bool allowed, int fileSize, string message)
        {
            Token = token;
            Allowed = allowed;
            FileSize = fileSize;
            Message = message;
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
        ///     Parses a new instance of <see cref="PeerTransferResponseIncoming"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PeerTransferResponseIncoming Parse(Message message)
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
            }
            else
            {
                msg = reader.ReadString();
            }

            return new PeerTransferResponseIncoming(token, allowed, fileSize, msg);
        }
    }
}