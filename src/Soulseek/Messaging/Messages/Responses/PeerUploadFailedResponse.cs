// <copyright file="PeerUploadFailedResponse.cs" company="JP Dillingham">
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
    ///     The response received when an attempt to queue a file for downloading has failed.
    /// </summary>
    internal sealed class PeerUploadFailedResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerUploadFailedResponse"/> class.
        /// </summary>
        /// <param name="filename">The filename which failed to be queued.</param>
        internal PeerUploadFailedResponse(string filename)
        {
            Filename = filename;
        }

        /// <summary>
        ///     Gets the filename which failed to be queued.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Parses a new instance of <see cref="PeerUploadFailedResponse"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static PeerUploadFailedResponse Parse(byte[] message)
        {
            var reader = new MessageReader<MessageCode.Peer>(message);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.UploadFailed)
            {
                throw new MessageException($"Message Code mismatch creating Peer Upload Failed Response (expected: {(int)MessageCode.Peer.UploadFailed}, received: {(int)code}.");
            }

            var filename = reader.ReadString();

            return new PeerUploadFailedResponse(filename);
        }
    }
}