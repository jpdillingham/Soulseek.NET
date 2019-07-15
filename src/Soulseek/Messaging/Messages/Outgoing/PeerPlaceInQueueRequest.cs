// <copyright file="PeerPlaceInQueueRequest.cs" company="JP Dillingham">
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
    /// <summary>
    ///     Requests the place of a file in a remote queue.
    /// </summary>
    internal sealed class PeerPlaceInQueueRequest
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerPlaceInQueueRequest"/> class.
        /// </summary>
        /// <param name="filename">The filename to check.</param>
        public PeerPlaceInQueueRequest(string filename)
        {
            Filename = filename;
        }

        /// <summary>
        ///     Gets the filename to check.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueRequest)
                .WriteString(Filename)
                .Build();
        }
    }
}