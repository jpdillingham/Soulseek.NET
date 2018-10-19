using System;
using System.Net;

namespace Soulseek.NET.Messaging.Login
{
    [Message(MessageCode.Login)]
    public class LoginResponse
    {
        public enum LoginResponseStatus : byte
        {
            Failure = 0,
            Success = 1,
        }

        public LoginResponseStatus Status { get; }
        public string Message { get; }
        public IPAddress IPAddress { get; }

        public LoginResponse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.Login)
            {
                throw new MessageException($"Message Code mismatch creating Login response (expected: {(int)MessageCode.Login}, received: {(int)reader.Code}");
            }

            Status = (LoginResponseStatus)reader.ReadByte();
            Message = reader.ReadString();

            if (Status == LoginResponseStatus.Success)
            {
                var ipBytes = reader.ReadBytes(4);
                Array.Reverse(ipBytes);
                IPAddress = new IPAddress(ipBytes);
            }
        }
    }
}
