// <copyright file="FolderContentsResponse.cs" company="JP Dillingham">
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
    using Soulseek.Exceptions;

    /// <summary>
    ///     The response to a peer folder contents request.
    /// </summary>
    internal sealed class FolderContentsResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FolderContentsResponse"/> class.
        /// </summary>
        /// <param name="token">The unique token for the request.</param>
        /// <param name="directory">The directory contents.</param>
        public FolderContentsResponse(int token, Directory directory)
        {
            Token = token;
            Directory = directory;
        }

        /// <summary>
        ///     Gets the directory contents.
        /// </summary>
        public Directory Directory { get; }

        /// <summary>
        ///     Gets the token for the response.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="BrowseResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static FolderContentsResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Peer>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Peer.FolderContentsResponse)
            {
                throw new MessageException($"Message Code mismatch creating folder contents response (expected: {(int)MessageCode.Peer.FolderContentsResponse}, received: {(int)code}");
            }

            reader.Decompress();

            var token = reader.ReadInteger();
            reader.ReadString(); // directory name, should always match that of the first directory
            reader.ReadInteger(); // directory count, should always be 1

            var dir = new Directory(
                directoryName: reader.ReadString(),
                fileCount: reader.ReadInteger());

            var fileList = new List<File>();

            for (int j = 0; j < dir.FileCount; j++)
            {
                var file = new File(
                    code: reader.ReadByte(),
                    filename: reader.ReadString(),
                    size: reader.ReadLong(),
                    extension: reader.ReadString(),
                    attributeCount: reader.ReadInteger());

                var attributeList = new List<FileAttribute>();

                for (int k = 0; k < file.AttributeCount; k++)
                {
                    var attribute = new FileAttribute(
                        type: (FileAttributeType)reader.ReadInteger(),
                        value: reader.ReadInteger());

                    attributeList.Add(attribute);
                }

                fileList.Add(new File(
                    code: file.Code,
                    filename: file.Filename,
                    size: file.Size,
                    extension: file.Extension,
                    attributeCount: file.AttributeCount,
                    attributeList: attributeList));
            }

            var directory = new Directory(
                directoryName: dir.DirectoryName,
                fileCount: dir.FileCount,
                fileList: fileList);

            return new FolderContentsResponse(token, directory);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Peer.FolderContentsResponse)
                .WriteInteger(Token)
                .WriteString(Directory.DirectoryName)
                .WriteInteger(1) // always one directory
                .WriteString(Directory.DirectoryName)
                .WriteInteger(Directory.FileCount);

            foreach (var file in Directory.Files)
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

            builder.Compress();
            return builder.Build();
        }
    }
}