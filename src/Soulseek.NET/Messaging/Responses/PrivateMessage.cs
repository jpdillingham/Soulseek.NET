// <copyright file="PrivateMessage.cs" company="JP Dillingham">
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

    public sealed class PrivateMessage
    {
        #region Public Properties

        public int Id { get; private set; }
        public bool IsAdmin { get; private set; }
        public string Message { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string Username { get; private set; }

        #endregion Public Properties

        #region Public Methods

        public static PrivateMessage Parse(Message message)
        {
            var response = new PrivateMessage();

            var reader = new MessageReader(message);

            response.Id = reader.ReadInteger();

            var timestamp = reader.ReadInteger();

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            response.Timestamp = epoch.AddSeconds(timestamp).ToLocalTime();

            response.Username = reader.ReadString();
            response.Message = reader.ReadString();
            response.IsAdmin = reader.ReadByte() == 1;

            return response;
        }

        #endregion Public Methods
    }
}