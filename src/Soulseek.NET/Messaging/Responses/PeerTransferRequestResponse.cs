using System;
using System.Collections.Generic;
using System.Text;

namespace Soulseek.NET.Messaging.Responses
{
    public sealed class PeerTransferRequestResponse
    {
        private PeerTransferRequestResponse()
        {
        }

        public TransferDirection Direction { get; private set; }
        public string Filename { get; private set; }
        public int Size { get; private set; }
        public int Token { get; private set; }

        public static PeerTransferRequestResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerTransferRequest)
            {
                throw new MessageException($"Message Code mismatch creating Peer Transfer Request response (expected: {(int)MessageCode.PeerTransferRequest}, received: {(int)reader.Code}.");
            }

            var response = new PeerTransferRequestResponse()
            {
                Direction = (TransferDirection)reader.ReadInteger(),
                Token = reader.ReadInteger(),
                Filename = reader.ReadString(),
                Size = reader.ReadInteger(),
            };

            return response;
        }
    }
}
