// <copyright file="Room.cs" company="JP Dillingham">
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

namespace Soulseek.NET
{
    /// <summary>
    ///     A chat room.
    /// </summary>
    public sealed class Room
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Room"/> class.
        /// </summary>
        /// <param name="name">The room name.</param>
        /// <param name="userCount">The number of users in the room.</param>
        internal Room(string name, int userCount)
        {
            Name = name;
            UserCount = userCount;
        }

        /// <summary>
        ///     Gets the name of the room.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///     Gets the number of users in the room.
        /// </summary>
        public int UserCount { get; }
    }
}