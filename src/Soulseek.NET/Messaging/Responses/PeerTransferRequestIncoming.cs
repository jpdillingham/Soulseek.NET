// <copyright file="PeerTransferRequestIncoming.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.NET.Messaging.Responses
{
    public sealed class PeerTransferRequestIncoming
    {
        private PeerTransferRequestIncoming()
        {
        }

        public TransferDirection Direction { get; private set; }
        public string Filename { get; private set; }
        public int FileSize { get; private set; }
        public int Token { get; private set; }

        public static PeerTransferRequestIncoming Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerTransferRequest)
            {
                throw new MessageException($"Message Code mismatch creating Peer Transfer Request response (expected: {(int)MessageCode.PeerTransferRequest}, received: {(int)reader.Code}.");
            }

            var response = new PeerTransferRequestIncoming()
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
