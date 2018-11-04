namespace Soulseek.NET.Messaging.Requests
{
    public class GetPeerAddressRequest
    {
        public string Username { get; set; }

        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.ServerGetPeerAddress)
                .WriteString(Username)
                .Build();
        }
    }
}
