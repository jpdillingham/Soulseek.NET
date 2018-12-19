// <copyright file="PeerTransferResponseIncoming.cs" company="JP Dillingham">
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
    using Soulseek.NET.Exceptions;

    public sealed class PeerTransferResponseIncoming
    {
        #region Private Constructors

        private PeerTransferResponseIncoming()
        {
        }

        #endregion Private Constructors

        #region Public Properties

        public int Token { get; private set; }
        public bool Allowed { get; private set; }
        public int FileSize { get; private set; }
        public string Message { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public static PeerTransferResponseIncoming Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.PeerTransferResponse)
            {
                throw new MessageException($"Message Code mismatch creating Peer Transfer Response (expected: {(int)MessageCode.PeerTransferResponse}, received: {(int)reader.Code}.");
            }

            var response = new PeerTransferResponseIncoming()
            {
                Token = reader.ReadInteger(),
                Allowed = reader.ReadByte() == 1 ? true : false,
            };

            if (response.Allowed)
            {
                response.FileSize = reader.ReadInteger();
            }
            else
            {
                response.Message = reader.ReadString();
            }

            return response;
        }

        #endregion Public Methods
    }
}