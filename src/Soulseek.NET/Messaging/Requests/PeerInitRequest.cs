namespace Soulseek.NET.Messaging.Requests
{ 
    public class PeerInitRequest
    {
        public PeerInitRequest(string username, string transferType, int token)
        {
            Username = username;
            TransferType = transferType;
            Token = token;
        }

        public string Username { get; set; }
        public string TransferType { get; set; }
        public int Token { get; set; }

        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .Code(0x1)
                .WriteString(Username)
                .WriteString(TransferType)
                .WriteInteger(300)
                .WriteInteger(Token)
                .Build()
                .ToByteArray();
        }
    }
}
