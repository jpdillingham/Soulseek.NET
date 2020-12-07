﻿// <copyright file="RoomInfo.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    ///     Information about a chat room.
    /// </summary>
    public class RoomInfo
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomInfo"/> class.
        /// </summary>
        /// <param name="name">The room name.</param>
        /// <param name="userCount">The number of users in the room.</param>
        /// <param name="isPrivate">A value indicating whether the room is private.</param>
        /// <param name="isModerated">A value indicating whether the included list of users are under moderation.</param>
        /// <param name="userList">The users in the room, if available.</param>
        public RoomInfo(string name, int userCount, bool isPrivate = false, bool isModerated = false, IEnumerable<string> userList = null)
        {
            Name = name;
            UserCount = userCount;
            IsPrivate = isPrivate;
            UserList = userList;
            IsModerated = isModerated;
        }

        /// <summary>
        ///     Gets a value indicating whether the room is private.
        /// </summary>
        public bool IsPrivate { get; }

        /// <summary>
        ///     Gets a value indicating whether the included list of users are under moderation.
        /// </summary>
        public bool IsModerated { get; }

        /// <summary>
        ///     Gets the room name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the number of users in the room.
        /// </summary>
        public int UserCount { get; }

        /// <summary>
        ///     Gets the users in the room, if available.
        /// </summary>
        public IReadOnlyCollection<string> Users => UserList?.ToList().AsReadOnly();

        private IEnumerable<string> UserList { get; }
    }
}