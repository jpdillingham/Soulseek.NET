using System;
using System.Net;

namespace Soulseek.NET.Messaging.Responses
{
    [MessageResponse(MessageCode.ServerConnectToPeer)]
    public class ConnectToPeerResponse : IMessageResponse<ConnectToPeerResponse>
    {
        public string Username { get; private set; }
        public string Type { get; private set; }
        public IPAddress IPAddress { get; private set; }
        public int Port { get; private set; }
        public int Token { get; private set; }

        public ConnectToPeerResponse Map(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerConnectToPeer)
            {
                throw new MessageException($"Message Code mismatch creating Connect To Peer response (expected: {(int)MessageCode.ServerConnectToPeer}, received: {(int)reader.Code}");
            }

            Username = reader.ReadString();
            Type = reader.ReadString();

            var ipBytes = reader.ReadBytes(4);
            Array.Reverse(ipBytes);
            IPAddress = new IPAddress(ipBytes);

            Port = reader.ReadInteger();
            Token = reader.ReadInteger();

            return this;
        }
    }
}
