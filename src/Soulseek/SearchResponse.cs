// <copyright file="SearchResponse.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System.Collections.Generic;
    using System.Linq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     A response to a file search.
    /// </summary>
    public class SearchResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the responding peer.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="fileCount">The number of files contained within the result, as counted by the original response.</param>
        /// <param name="freeUploadSlots">The number of free upload slots for the peer.</param>
        /// <param name="uploadSpeed">The upload speed of the peer.</param>
        /// <param name="queueLength">The length of the peer's upload queue.</param>
        /// <param name="fileList">The optional file list.</param>
        public SearchResponse(string username, int token, int fileCount, int freeUploadSlots, int uploadSpeed, long queueLength, IEnumerable<File> fileList = null)
        {
            Username = username;
            Token = token;
            FileCount = fileCount;
            FreeUploadSlots = freeUploadSlots;
            UploadSpeed = uploadSpeed;
            QueueLength = queueLength;
            FileList = fileList ?? Enumerable.Empty<File>();
        }

        internal SearchResponse(SearchResponse searchResponse, IEnumerable<File> fileList)
            : this(searchResponse.Username, searchResponse.Token, fileList.Count(), searchResponse.FreeUploadSlots, searchResponse.UploadSpeed, searchResponse.QueueLength, fileList)
        {
        }

        /// <summary>
        ///     Gets the number of files contained within the result, as counted by the original response from the peer and prior
        ///     to filtering. For the filtered count, check the length of <see cref="Files"/>.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Gets the list of files.
        /// </summary>
        public IReadOnlyCollection<File> Files => FileList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the number of free upload slots for the peer.
        /// </summary>
        public int FreeUploadSlots { get; }

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

        private IEnumerable<File> FileList { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="SearchResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        internal static SearchResponse FromByteArray(byte[] bytes)
        {
            var slim = SearchResponseSlim.FromByteArray(bytes);
            return FromSlimResponse(slim);
        }

        internal static SearchResponse FromSlimResponse(SearchResponseSlim slim)
        {
            var files = ParseFiles(slim.MessageReader, slim.FileCount);
            return new SearchResponse(slim.Username, slim.Token, files.Count, slim.FreeUploadSlots, slim.UploadSpeed, slim.QueueLength, files);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        internal byte[] ToByteArray()
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(Username)
                .WriteInteger(Token)
                .WriteInteger(FileCount);

            foreach (var file in Files)
            {
                builder
                    .WriteByte((byte)file.Code)
                    .WriteString(file.Filename)
                    .WriteLong(file.Size)
                    .WriteString(file.Extension)
                    .WriteInteger(file.AttributeCount);

                foreach (var attribute in file.Attributes)
                {
                    builder
                        .WriteInteger((int)attribute.Type)
                        .WriteInteger(attribute.Value);
                }
            }

            builder
                .WriteByte((byte)FreeUploadSlots)
                .WriteInteger(UploadSpeed)
                .WriteLong(QueueLength);

            builder.Compress();
            return builder.Build();
        }

        /// <summary>
        ///     Parses the list of files contained within the <paramref name="reader"/>.
        /// </summary>
        /// <remarks>
        ///     Requires that the provided MessageReader has been "rewound" to the start of the file list, which is equal to the
        ///     length of the username plus 12.
        /// </remarks>
        /// <param name="reader">The reader from which to parse the file list.</param>
        /// <param name="count">The expected number of files.</param>
        /// <returns>The list of parsed files.</returns>
        private static IReadOnlyCollection<File> ParseFiles(MessageReader<MessageCode.Peer> reader, int count)
        {
            var files = new List<File>();

            for (int i = 0; i < count; i++)
            {
                var file = new File(
                    code: reader.ReadByte(),
                    filename: reader.ReadString(),
                    size: reader.ReadLong(),
                    extension: reader.ReadString(),
                    attributeCount: reader.ReadInteger());

                var attributeList = new List<FileAttribute>();

                for (int j = 0; j < file.AttributeCount; j++)
                {
                    var attribute = new FileAttribute(
                        type: (FileAttributeType)reader.ReadInteger(),
                        value: reader.ReadInteger());

                    attributeList.Add(attribute);
                }

                files.Add(new File(
                    code: file.Code,
                    filename: file.Filename,
                    size: file.Size,
                    extension: file.Extension,
                    attributeCount: file.AttributeCount,
                    attributeList: attributeList));
            }

            return files.AsReadOnly();
        }
    }
}