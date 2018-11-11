// <copyright file="LoginResponse.cs" company="JP Dillingham">
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

    public sealed class LoginResponse
    {
        #region Private Constructors

        private LoginResponse()
        {
        }

        #endregion Private Constructors

        #region Public Properties

        public string IPAddress { get; private set; }
        public string Message { get; private set; }
        public bool Succeeded { get; private set; }

        #endregion Public Properties

        #region Public Methods

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

        #endregion Public Methods
    }
}