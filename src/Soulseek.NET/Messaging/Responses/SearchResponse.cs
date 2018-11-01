namespace Soulseek.NET.Messaging.Responses
{
    using Soulseek.NET.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public sealed class SearchResponse
    {
        public string Username { get; private set; }
        public int Ticket { get; private set; }
        public int FileCount { get; private set; }
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
        public int FreeUploadSlots { get; private set; }
        public int UploadSpeed { get; private set; }
        public long QueueLength { get; private set; }

        private MessageReader MessageReader { get; set; }
        private List<File> FileList { get; set; }

        internal void ParseFiles()
        {
            FileList = ParseFiles(MessageReader, FileCount);
        }

        internal void SetFiles(IEnumerable<File> files)
        {
            FileList = files.ToList();
        }

        internal SearchResponse()
        {
        }

        private static List<File> ParseFiles(MessageReader reader, int count)
        {
            var files = new List<File>();

            for (int i = 0; i < count; i++)
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

                files.Add(file);
            }

            return files;
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

            var position = reader.Position;
            
            reader.Seek(reader.Payload.Length - 17); // there are 8 unused bytes at the end of each message

            response.FreeUploadSlots = reader.ReadByte();
            response.UploadSpeed = reader.ReadInteger();
            response.QueueLength = reader.ReadLong();

            reader.Seek(position);
            response.MessageReader = reader;

            return response;
        }
    }
}