namespace Soulseek.NET.Messaging.Responses
{
    using System;
    using System.Net;

    public sealed class ConnectToPeerResponse
    {
        public string Username { get; private set; }
        public string Type { get; private set; }
        public IPAddress IPAddress { get; private set; }
        public int Port { get; private set; }
        public int Token { get; private set; }

        public static ConnectToPeerResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerConnectToPeer)
            {
                throw new MessageException($"Message Code mismatch creating Connect To Peer response (expected: {(int)MessageCode.ServerConnectToPeer}, received: {(int)reader.Code}");
            }

            var response = new ConnectToPeerResponse
            {
                Username = reader.ReadString(),
                Type = reader.ReadString()
            };

            var ipBytes = reader.ReadBytes(4);
            Array.Reverse(ipBytes);
            response.IPAddress = new IPAddress(ipBytes);

            response.Port = reader.ReadInteger();
            response.Token = reader.ReadInteger();

            return response;
        }

        private ConnectToPeerResponse()
        {
        }
    }
}
