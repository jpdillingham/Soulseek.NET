using System;

namespace Soulseek.NET.Messaging.Responses
{
    public sealed class PrivateMessage
    {
        public int Id { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string Username { get; private set; }
        public string Message { get; private set; }
        public bool IsAdmin { get; private set; }

        public static PrivateMessage Parse(Message message)
        {
            var response = new PrivateMessage();

            var reader = new MessageReader(message);

            response.Id = reader.ReadInteger();

            var timestamp = reader.ReadInteger();

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            response.Timestamp = epoch.AddSeconds(timestamp).ToLocalTime();

            response.Username = reader.ReadString();
            response.Message = reader.ReadString();
            response.IsAdmin = reader.ReadByte() == 1;

            return response;
        }
    }
}
