namespace Soulseek.NET.Messaging.Responses
{
    using System;
    using System.Collections.Generic;

    public sealed class SearchResponse
    {
        public string Username { get; private set; }
        public int Ticket { get; private set; }
        public int FileCount { get; private set; }
        public IEnumerable<File> Files => FileList.AsReadOnly();
        public int FreeUploadSlots { get; private set; }
        public int UploadSpeed { get; private set; }
        public int InQueue { get; private set; }

        private List<File> FileList { get; set; } = new List<File>();

        private SearchResponse()
        {
        }

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
                Ticket = reader.ReadInteger(),
                FileCount = reader.ReadInteger()
            };

            for (int i = 0; i < response.FileCount; i++)
            {
                var file = new File
                {
                    Code = reader.ReadByte(),
                    Filename = reader.ReadString(),
                    Size = reader.ReadLong(),
                    Extension = reader.ReadString(),
                    AttributeCount = reader.ReadInteger()
                };

                for (int j = 0; j < file.AttributeCount; j++)
                {
                    var attribute = new FileAttribute
                    {
                        Type = (FileAttributeType)reader.ReadInteger(),
                        Value = reader.ReadInteger()
                    };
                    ((List<FileAttribute>)file.Attributes).Add(attribute);
                }

                response.FileList.Add(file);
            }

            response.FreeUploadSlots = reader.ReadByte();
            response.UploadSpeed = reader.ReadInteger();
            response.InQueue = reader.ReadInteger();

            return response;
        }
    }

    public sealed class File
    {
        public int Code { get; internal set; }
        public string Filename { get; internal set; }
        public long Size { get; internal set; }
        public string Extension { get; internal set; }
        public int AttributeCount { get; internal set; }
        public IEnumerable<FileAttribute> Attributes { get; internal set; } = new List<FileAttribute>();

        internal File()
        {
        }
    }

    public sealed class FileAttribute
    {
        public FileAttributeType Type { get; internal set; }
        public int Value { get; internal set; }

        internal FileAttribute()
        {
        }
    }

    public enum FileAttributeType
    {
        BitRate = 0,
        Length = 1,
        Unknown = 2,
        SampleRate = 4,
        BitDepth = 5,
    }
}