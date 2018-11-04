namespace Soulseek.NET.Messaging.Requests
{ 
    public class PeerSharesRequest
    {
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .Code(MessageCode.PeerSharesRequest)
                .Build()
                .ToByteArray();
        }
    }
}
