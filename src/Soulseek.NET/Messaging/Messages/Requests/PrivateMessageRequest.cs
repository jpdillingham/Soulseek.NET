namespace Soulseek.NET.Messaging.Messages
{
    internal class PrivateMessageRequest
    {
        public PrivateMessageRequest(string username, string message)
        {
            Username = username;
            Message = message;
        }

        public string Username { get; }
        public string Message { get; }

        internal Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.ServerPrivateMessage)
                .WriteString(Username)
                .WriteString(Message)
                .Build();
        }
    }
}
