// <copyright file="SearchResponseSlim.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Messaging.Responses
{
    using Soulseek.NET.Exceptions;

    /// <summary>
    ///     A response to a file search which does not include a parsed list of files. This internal class allows the library to
    ///     defer processing of file entries until the other information in the response has been matched with criteria to
    ///     determine whether the response is to be thrown out.
    /// </summary>
    /// <remarks>Files may be retrieved using the message reader provided by <see cref="MessageReader"/>.</remarks>
    internal sealed class SearchResponseSlim
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponseSlim"/> class.
        /// </summary>
        /// <param name="username">The username of the responding peer.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="fileCount">The number of files contained within the result.</param>
        /// <param name="freeUploadSlots">The number of free upload slots for the peer.</param>
        /// <param name="uploadSpeed">The upload speed of the peer.</param>
        /// <param name="queueLength">The length of the peer's upload queue.</param>
        /// <param name="messageReader">The MessageReader instance used to parse the file list.</param>
        internal SearchResponseSlim(string username, int token, int fileCount, int freeUploadSlots, int uploadSpeed, long queueLength, MessageReader messageReader)
        {
            Username = username;
            Token = token;
            FileCount = fileCount;
            FreeUploadSlots = freeUploadSlots;
            UploadSpeed = uploadSpeed;
            QueueLength = queueLength;
            MessageReader = messageReader;
        }

        /// <summary>
        ///     Gets the number of files contained within the result.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the number of free upload slots for the peer.
        /// </summary>
        public int FreeUploadSlots { get; }

        /// <summary>
        ///     Gets the MessageReader instance used to parse the file list.
        /// </summary>
        public MessageReader MessageReader { get; }

        /// <summary>
        ///     Gets the length of the peer's upload queue.
        /// </summary>
        public long QueueLength { get; }

        /// <summary>
        ///     Gets the unique search token.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the upload speed of the peer.
        /// </summary>
        public int UploadSpeed { get; }

        /// <summary>
        ///     Gets the username of the responding peer.
        /// </summary>
        public string Username { get; }

        /// <summary>
        ///     Parses a new instance of <see cref="SearchResponseSlim"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        internal static SearchResponseSlim Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerSearchResponse)
            {
                throw new MessageException($"Message Code mismatch creating Peer Search Response (expected: {(int)MessageCode.PeerSearchResponse}, received: {(int)reader.Code}");
            }

            reader.Decompress();

            var username = reader.ReadString();
            var token = reader.ReadInteger();
            var fileCount = reader.ReadInteger();

            // the following properties are positioned at the end of the response, past the files. there are 4 unused (or unknown)
            // bytes at the end of the message. seek the reader past the files.
            var position = reader.Position;
            reader.Seek(reader.Payload.Length - 17);

            var freeUploadSlots = reader.ReadByte();
            var uploadSpeed = reader.ReadInteger();
            var queueLength = reader.ReadLong();

            // seek the reader back to the start of the file list.
            reader.Seek(position);
            var messageReader = reader;

            return new SearchResponseSlim(username, token, fileCount, freeUploadSlots, uploadSpeed, queueLength, messageReader);
        }
    }
}