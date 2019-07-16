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

namespace Soulseek.Messaging.Messages
{
    using System.Collections.Generic;
    using Soulseek.Exceptions;

    /// <summary>
    ///     A list of available chat rooms.
    /// </summary>
    internal static class RoomList
    {
        /// <summary>
        ///     Creates a new list of rooms from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static IReadOnlyCollection<(string Name, int UserCount)> FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.RoomList)
            {
                throw new MessageException($"Message Code mismatch creating Room List response (expected: {(int)MessageCode.Server.RoomList}, received: {(int)code}");
            }

            var roomCount = reader.ReadInteger();
            var roomNames = new List<string>();

            for (int i = 0; i < roomCount; i++)
            {
                roomNames.Add(reader.ReadString());
            }

            var userCountCount = reader.ReadInteger();
            var rooms = new List<(string Name, int UserCount)>();

            for (int i = 0; i < userCountCount; i++)
            {
                var count = reader.ReadInteger();
                rooms.Add((roomNames[i], count));
            }

            return rooms.AsReadOnly();
        }
    }
}