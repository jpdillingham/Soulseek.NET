using System;
using System.Net;

namespace Soulseek.NET.Messaging.Login
{
    [MessageResponse(MessageCode.Login)]
    public class LoginResponse
    {
        public enum LoginResponseStatus : byte
        {
            Failure = 0,
            Success = 1,
        }

        public LoginResponseStatus Status { get; set; }
        public string Message { get; set; }
        public IPAddress IPAddress { get; set; }

        public LoginResponse(byte[] bytes)
        {
            var reader = new MessageReader(bytes);

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
