// <copyright file="SearchResponseResponse.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     A response to a file search.
    /// </summary>
    internal sealed class SearchResponseResponse : SearchResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponseResponse"/> class.
        /// </summary>
        /// <param name="username">The username of the responding peer.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="fileCount">The number of files contained within the result, as counted by the original response.</param>
        /// <param name="freeUploadSlots">The number of free upload slots for the peer.</param>
        /// <param name="uploadSpeed">The upload speed of the peer.</param>
        /// <param name="queueLength">The length of the peer's upload queue.</param>
        /// <param name="fileList">The optional file list.</param>
        public SearchResponseResponse(string username, int token, int fileCount, int freeUploadSlots, int uploadSpeed, long queueLength, IEnumerable<File> fileList = null)
            : base(username, token, fileCount, freeUploadSlots, uploadSpeed, queueLength, fileList)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponseResponse"/> class.
        /// </summary>
        /// <param name="slimResponse">The SearchResponseSlim instance from which to initialize this SearchResponse.</param>
        public SearchResponseResponse(SearchResponseResponseSlim slimResponse)
            : this(slimResponse.Username, slimResponse.Token, slimResponse.FileCount, slimResponse.FreeUploadSlots, slimResponse.UploadSpeed, slimResponse.QueueLength, ParseFiles(slimResponse.MessageReader, slimResponse.FileCount))
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SearchResponseResponse"/> class.
        /// </summary>
        /// <param name="response">The SearchResponse instance from which to initialize this SearchResponse.</param>
        /// <param name="fileList">The file list with which to replace the file list in the specified <paramref name="response"/>.</param>
        public SearchResponseResponse(SearchResponseResponse response, IEnumerable<File> fileList)
            : this(response.Username, response.Token, fileList.Count(), response.FreeUploadSlots, response.UploadSpeed, response.QueueLength, fileList)
        {
        }

        /// <summary>
        ///     Creates a new instance of <see cref="SearchResponseResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static SearchResponseResponse FromByteArray(byte[] bytes)
        {
            var slim = SearchResponseResponseSlim.FromByteArray(bytes);
            return new SearchResponseResponse(slim);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
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