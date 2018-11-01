namespace Soulseek.NET.Messaging.Responses
{
    using Soulseek.NET.Common;
    using System;
    using System.Net;

    public sealed class LoginResponse
    {
        public bool Succeeded { get; private set; }
        public bool Failed => !Succeeded;
        public string Message { get; private set; }
        public string IPAddress { get; private set; }

        public static LoginResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerLogin)
            {
                throw new MessageException($"Message Code mismatch creating Login response (expected: {(int)MessageCode.ServerLogin}, received: {(int)reader.Code}");
            }

            var response = new LoginResponse
            {
                Succeeded = reader.ReadByte() == 1,
                Message = reader.ReadString()
            };

            if (response.Succeeded)
            {
                var ipBytes = reader.ReadBytes(4);
                Array.Reverse(ipBytes);
                response.IPAddress = new IPAddress(ipBytes).ToString();
            }

            return response;
        }

        private LoginResponse()
        {
        }
    }
}
