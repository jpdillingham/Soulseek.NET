namespace Soulseek.NET.Messaging.Requests
{ 
    public class LoginRequest
    {
        public LoginRequest(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public string Username { get; set; }
        public string Password { get; set; }
        public int Version => 181;
        public string Hash => $"{Username}{Password}".ToMD5Hash();
        public int MinorVersion => 1;

        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.ServerLogin)
                .WriteString(Username)
                .WriteString(Password)
                .WriteInteger(Version)
                .WriteString(Hash)
                .WriteInteger(MinorVersion)
                .Build();
        }
    }
}
