// <copyright file="RoomList.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using Soulseek.NET.Exceptions;

    public static class RoomList
    {
        #region Public Methods

        public static IEnumerable<Room> Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerRoomList)
            {
                throw new MessageException($"Message Code mismatch creating Room List response (expected: {(int)MessageCode.ServerRoomList}, received: {(int)reader.Code}");
            }

            var roomCount = reader.ReadInteger();
            var list = new List<Room>();

            for (int i = 0; i < roomCount; i++)
            {
                list.Add(new Room() { Name = reader.ReadString() });
            }

            var userCountCount = reader.ReadInteger();

            for (int i = 0; i < userCountCount; i++)
            {
                list[i].UserCount = reader.ReadInteger();
            }

            return list.AsReadOnly();
        }

        #endregion Public Methods
    }
}