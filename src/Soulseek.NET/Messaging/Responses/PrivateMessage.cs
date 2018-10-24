using System;

namespace Soulseek.NET.Messaging.Responses
{
    public sealed class PrivateMessage
    {
        public int Id { get; private set; }
        public int Timestamp { get; private set; }
        public string Username { get; private set; }
        public string Message { get; private set; }
        public bool IsAdmin { get; private set; }

        public static PrivateMessage Parse(Message message)
        {
            var response = new PrivateMessage();

            var reader = new MessageReader(message);

            response.Id = reader.ReadInteger();
            response.Timestamp = reader.ReadInteger();
            response.Username = reader.ReadString();
            response.Message = reader.ReadString();
            response.IsAdmin = reader.ReadByte() == 1;

            return response;
        }
    }
}
