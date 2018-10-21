using System.Collections.Generic;
using System.Linq;

namespace Soulseek.NET.Messaging.Responses
{
    [MessageResponse(MessageCode.PeerSearchReply)]
    public class PeerSearchReply : IMessageResponse<PeerSearchReply>
    {
        public string Username { get; private set; }
        public int Ticket { get; private set; }
        public int FileCount { get; private set; }
        public IEnumerable<File> Files => FileList;
        public int FreeUploadSlots { get; private set; }
        public int UploadSpeed { get; set; }
        public int InQueue { get; set; }

        public List<File> FileList { get; private set; } = new List<File>();

        public PeerSearchReply Map(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerSearchReply)
            {
                throw new MessageException($"Message Code mismatch creating Peer Search Reply (expected: {(int)MessageCode.PeerSearchReply}, received: {(int)reader.Code}");
            }

            reader.Decompress();

            Username = reader.ReadString();
            Ticket = reader.ReadInteger();
            FileCount = reader.ReadInteger();

            for (int i = 0; i < FileCount; i++)
            {
                var file = new File();

                file.Code = reader.ReadByte();
                file.Filename = reader.ReadString();
                file.Size = reader.ReadInteger();
                file.Extension = reader.ReadString();
                file.AttributeCount = reader.ReadInteger();

                var attributes = file.Attributes.ToList();

                for (int j = 0; j < file.AttributeCount; j++)
                {
                    var attribute = new FileAttribute();
                    attribute.Type = reader.ReadInteger();
                    attribute.Value = reader.ReadInteger();

                    attributes.Add(attribute);
                }

                FileList.Add(file);
            }

            FreeUploadSlots = reader.ReadByte();
            UploadSpeed = reader.ReadInteger();
            InQueue = reader.ReadInteger();

            return this;
        }
    }
}
