namespace Soulseek.NET.Messaging.Responses
{
    using System;
    using System.Net;

    [MessageResponse(MessageCode.ServerLogin)]
    public class LoginResponse : IMessageResponse<LoginResponse>
    {
        public bool Succeeded { get; private set; }
        public bool Failed => !Succeeded;
        public string Message { get; private set; }
        public string IPAddress { get; private set; }

        public LoginResponse Map(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerLogin)
            {
                throw new MessageException($"Message Code mismatch creating Login response (expected: {(int)MessageCode.ServerLogin}, received: {(int)reader.Code}");
            }

            Succeeded = reader.ReadByte() == 1;
            Message = reader.ReadString();

            if (Succeeded)
            {
                var ipBytes = reader.ReadBytes(4);
                Array.Reverse(ipBytes);
                IPAddress = new IPAddress(ipBytes).ToString();
            }

            return this;
        }
    }
}
