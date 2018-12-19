// <copyright file="GetPeerAddressResponse.cs" company="JP Dillingham">
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
    using System;
    using System.Net;
    using Soulseek.NET.Exceptions;

    public sealed class GetPeerAddressResponse
    {
        #region Private Constructors

        private GetPeerAddressResponse()
        {
        }

        #endregion Private Constructors

        #region Public Properties

        public IPAddress IPAddress { get; private set; }
        public int Port { get; private set; }
        public string Username { get; private set; }

        #endregion Public Properties

        #region Public Methods

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
            response.IPAddress = new IPAddress(ipBytes);

            response.Port = reader.ReadInteger();

            return response;
        }

        #endregion Public Methods
    }
}