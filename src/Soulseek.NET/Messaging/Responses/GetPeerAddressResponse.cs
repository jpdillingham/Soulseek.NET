namespace Soulseek.NET.Messaging.Responses
{
    using System;
    using System.Net;

    public sealed class GetPeerAddressResponse
    {
        public string Username { get; private set; }
        public string IPAddress { get; private set; }
        public int Port { get; private set; }
        
        public static GetPeerAddressResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerGetPeerAddress)
            {
                throw new MessageException($"Message Code mismatch creating Get Peer Address response (expected: {(int)MessageCode.ServerGetPeerAddress}, received: {(int)reader.Code}.");
            }

            var response = new GetPeerAddressResponse()
            {
                Username = reader.ReadString(),
            };

            var ipBytes = reader.ReadBytes(4);
            Array.Reverse(ipBytes);
            response.IPAddress = new IPAddress(ipBytes).ToString();

            response.Port = reader.ReadInteger();

            return response;
        }

        private GetPeerAddressResponse()
        {
        }
    }
}
