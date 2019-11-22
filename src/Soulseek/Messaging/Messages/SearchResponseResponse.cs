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
    using Soulseek.Messaging;

    /// <summary>
    ///     The response to a file search request.
    /// </summary>
    internal static class SearchResponseResponse
    {
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

        /// <summary>
        ///     Creates a new instance of <see cref="SearchResponse"/> from the specified <paramref name="slimResponse"/>.
        /// </summary>
        /// <param name="slimResponse">The slim response from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        internal static SearchResponse FromSlimResponse(SearchResponseSlim slimResponse)
        {
            var files = ParseFiles(slimResponse.MessageReader, slimResponse.FileCount);
            return new SearchResponse(slimResponse.Username, slimResponse.Token, files.Count, slimResponse.FreeUploadSlots, slimResponse.UploadSpeed, slimResponse.QueueLength, files);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <param name="searchResponse">The instance from which to construct the byte array.</param>
        /// <returns>The constructed byte array.</returns>
        internal static byte[] ToByteArray(this SearchResponse searchResponse)
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(searchResponse.Username)
                .WriteInteger(searchResponse.Token)
                .WriteInteger(searchResponse.FileCount);

            foreach (var file in searchResponse.Files)
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
                .WriteByte((byte)searchResponse.FreeUploadSlots)
                .WriteInteger(searchResponse.UploadSpeed)
                .WriteLong(searchResponse.QueueLength);

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