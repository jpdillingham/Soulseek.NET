namespace Soulseek.NET.Messaging.Requests
{ 
    public class AcknowledgePrivateMessageRequest
    {
        public AcknowledgePrivateMessageRequest(int id)
        {
            Id = id;
        }

        public int Id { get; set; }

        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .Code(MessageCode.ServerAcknowledgePrivateMessage)
                .WriteInteger(Id)
                .Build()
                .ToByteArray();
        }
    }
}
