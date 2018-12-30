// <copyright file="SearchResponse.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Soulseek.NET.Exceptions;

    public sealed class SearchResponse
    {
        #region Internal Constructors

        internal SearchResponse()
        {
        }

        #endregion Internal Constructors

        #region Public Properties

        public int FileCount { get; internal set; }

        public IEnumerable<File> Files
        {
            get
            {
                return FileList.AsReadOnly();
            }

            internal set
            {
                FileList = value.ToList();
            }
        }

        public int FreeUploadSlots { get; internal set; }
        public long QueueLength { get; internal set; }
        public int Token { get; internal set; }
        public int UploadSpeed { get; internal set; }
        public string Username { get; internal set; }

        #endregion Public Properties

        #region Private Properties

        private List<File> FileList { get; set; }
        private MessageReader MessageReader { get; set; }

        #endregion Private Properties

        #region Public Methods

        public static SearchResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerSearchResponse)
            {
                throw new MessageException($"Message Code mismatch creating Peer Search Response (expected: {(int)MessageCode.PeerSearchResponse}, received: {(int)reader.Code}");
            }

            try
            {
                reader.Decompress();
            }
            catch (Exception)
            {
                // discard result if it fails to decompress
                return null;
            }

            var response = new SearchResponse
            {
                Username = reader.ReadString(),
                Token = reader.ReadInteger(),
                FileCount = reader.ReadInteger()
            };

            var position = reader.Position;

            reader.Seek(reader.Payload.Length - 17); // there are 8 unused bytes at the end of each message

            response.FreeUploadSlots = reader.ReadByte();
            response.UploadSpeed = reader.ReadInteger();
            response.QueueLength = reader.ReadLong();

            reader.Seek(position);
            response.MessageReader = reader;

            return response;
        }

        #endregion Public Methods

        #region Internal Methods

        internal void ParseFiles()
        {
            FileList = ParseFiles(MessageReader, FileCount);
        }

        #endregion Internal Methods

        #region Private Methods

        private static List<File> ParseFiles(MessageReader reader, int count)
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

            return files;
        }

        #endregion Private Methods
    }
}