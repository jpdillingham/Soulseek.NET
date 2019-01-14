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

namespace Soulseek.NET.Messaging.Messages
{
    using System.Collections.Generic;
    using Soulseek.NET.Exceptions;

    /// <summary>
    ///     A list of available chat rooms.
    /// </summary>
    public static class RoomList
    {
        /// <summary>
        ///     Parses a new instance of <see cref="RoomList"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static IEnumerable<Room> Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerRoomList)
            {
                throw new MessageException($"Message Code mismatch creating Room List response (expected: {(int)MessageCode.ServerRoomList}, received: {(int)reader.Code}");
            }

            var roomCount = reader.ReadInteger();
            var roomNames = new List<string>();

            for (int i = 0; i < roomCount; i++)
            {
                roomNames.Add(reader.ReadString());
            }

            var userCountCount = reader.ReadInteger();
            var rooms = new List<Room>();

            for (int i = 0; i < userCountCount; i++)
            {
                var count = reader.ReadInteger();
                rooms.Add(new Room(roomNames[i], count));
            }

            return rooms.AsReadOnly();
        }
    }
}