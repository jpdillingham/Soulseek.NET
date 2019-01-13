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
    using System.Collections.Generic;

    public sealed class SearchResponse
    {
        public SearchResponse(string username, int token, int fileCount, int freeUploadSlots, int uploadSpeed, long queueLength, List<File> fileList = null)
        {
            Username = username;
            Token = token;
            FileCount = fileCount;
            FreeUploadSlots = freeUploadSlots;
            UploadSpeed = uploadSpeed;
            QueueLength = queueLength;
            FileList = fileList ?? new List<File>();
        }

        internal SearchResponse(SearchResponseSlim slimResponse)
            : this(slimResponse.Username, slimResponse.Token, slimResponse.FileCount, slimResponse.FreeUploadSlots, slimResponse.UploadSpeed, slimResponse.QueueLength)
        {
            FileList = ParseFiles(slimResponse.MessageReader, slimResponse.FileCount);
        }

        internal SearchResponse(SearchResponse response, List<File> fileList)
            : this(response.Username, response.Token, response.FileCount, response.FreeUploadSlots, response.UploadSpeed, response.QueueLength, fileList)
        {
        }

        public int FileCount { get; }

        public IEnumerable<File> Files => FileList.AsReadOnly();

        public int FreeUploadSlots { get; }
        public long QueueLength { get; }
        public int Token { get; }
        public int UploadSpeed { get; }
        public string Username { get; }

        private List<File> FileList { get; set; }

        public static SearchResponse Parse(Message message)
        {
            var slim = SearchResponseSlim.Parse(message);
            return new SearchResponse(slim);
        }

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

    }
}