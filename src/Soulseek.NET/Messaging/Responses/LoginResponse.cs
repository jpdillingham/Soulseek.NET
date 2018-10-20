using System;
using System.Collections.Generic;
using System.Net;

namespace Soulseek.NET.Messaging.Responses
{
    [MessageResponse(MessageCode.ServerLogin)]
    public class LoginResponse : IMessageResponse<LoginResponse>
    {
        public bool Succeeded { get; private set; }
        public bool Failed => !Succeeded;
        public string Message { get; private set; }
        public string IPAddress { get; private set; }
        public IEnumerable<Room> Rooms { get; set; }
        public IEnumerable<string> PrivilegedUsers { get; set; }

        public LoginResponse MapFrom(Message message)
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
