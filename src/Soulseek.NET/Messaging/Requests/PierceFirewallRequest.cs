namespace Soulseek.NET.Messaging.Requests
{ 
    public class PierceFirewallRequest
    {
        public PierceFirewallRequest(int token)
        {
            Token = token;
        }

        public int Token { get; set; }

        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .Code((byte)0x0)
                .WriteInteger(Token)
                .Build()
                .ToByteArray();
        }
    }
}
