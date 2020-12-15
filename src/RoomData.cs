// <copyright file="RoomData.cs" company="JP Dillingham">
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
    ///     The response to request to join a chat room.
    /// </summary>
    public class RoomData
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomData"/> class.
        /// </summary>
        /// <param name="name">The name of the room that was joined.</param>
        /// <param name="userCount">The number of users in the room.</param>
        /// <param name="userList">The users in the room.</param>
        /// <param name="isPrivate">A value indicating whether the room is private.</param>
        /// <param name="owner">The owner of the room, if private.</param>
        /// <param name="operatorCount">The number of operators in the room, if private.</param>
        /// <param name="operatorList">The operators in the room, if private.</param>
        public RoomData(string name, int userCount, IEnumerable<UserData> userList, bool isPrivate = false, string owner = null, int? operatorCount = null, IEnumerable<string> operatorList = null)
        {
            Name = name;
            UserCount = userCount;
            UserList = userList ?? Enumerable.Empty<UserData>();
            IsPrivate = isPrivate;
            Owner = owner;
            OperatorCount = operatorCount;
            OperatorList = operatorList ?? Enumerable.Empty<string>();
        }

        /// <summary>
        ///     Gets a value indicating whether the room is private.
        /// </summary>
        public bool IsPrivate { get; }

        /// <summary>
        ///     Gets the name of the room that was joined.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the number of operators in the room, if private.
        /// </summary>
        public int? OperatorCount { get; }

        /// <summary>
        ///     Gets the operators in the room, if private.
        /// </summary>
        public IReadOnlyCollection<string> Operators => OperatorList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the owner of the room, if private.
        /// </summary>
        public string Owner { get; }

        /// <summary>
        ///     Gets the number of users in the room.
        /// </summary>
        public int UserCount { get; }

        /// <summary>
        ///     Gets the users in the room.
        /// </summary>
        public IReadOnlyCollection<UserData> Users => UserList.ToList().AsReadOnly();

        private IEnumerable<string> OperatorList { get; }
        private IEnumerable<UserData> UserList { get; }
    }
}