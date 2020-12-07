// <copyright file="RoomList.cs" company="JP Dillingham">
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
    public class RoomList
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomList"/> class.
        /// </summary>
        /// <param name="publicCount">The number of public rooms.</param>
        /// <param name="publicList">The list of public rooms.</param>
        /// <param name="privateCount">The number of private rooms.</param>
        /// <param name="privateList">The list of private rooms.</param>
        /// <param name="ownedCount">The number of rooms owned by the currently logged in user.</param>
        /// <param name="ownedList">The list of rooms owned by the currently logged in user.</param>
        /// <param name="moderatedNameCount">The number of rooms in which the currently logged in user has moderator status.</param>
        /// <param name="moderatedNameList">The list of room names in which the currently logged in user has moderator status.</param>
        public RoomList(
            int publicCount,
            IEnumerable<RoomInfo> publicList,
            int privateCount,
            IEnumerable<RoomInfo> privateList,
            int ownedCount,
            IEnumerable<RoomInfo> ownedList,
            int moderatedNameCount,
            IEnumerable<string> moderatedNameList)
        {
            PublicCount = publicCount;
            PublicList = publicList ?? Enumerable.Empty<RoomInfo>();
            PrivateCount = privateCount;
            PrivateList = privateList ?? Enumerable.Empty<RoomInfo>();
            OwnedCount = ownedCount;
            OwnedList = ownedList ?? Enumerable.Empty<RoomInfo>();
            ModeratedNameCount = moderatedNameCount;
            ModeratedNameList = moderatedNameList ?? Enumerable.Empty<string>();
        }

        /// <summary>
        ///     Gets the number of public rooms.
        /// </summary>
        public int PublicCount { get; }

        /// <summary>
        ///     Gets the number of private rooms.
        /// </summary>
        public int PrivateCount { get; }

        /// <summary>
        ///     Gets the number of rooms owned by the currently logged in user.
        /// </summary>
        public int OwnedCount { get; }

        /// <summary>
        ///     Gets the number of room names in which the currently logged in user has moderator status.
        /// </summary>
        public int ModeratedNameCount { get; }

        /// <summary>
        ///     Gets the list of public rooms.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Public => PublicList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the list of private rooms.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Private => PrivateList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the list of rooms owned by the currently logged in user.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Owned => OwnedList.ToList().AsReadOnly();

        /// <summary>
        ///     Gets the list of room names in which the currently logged in user has moderator status.
        /// </summary>
        public IReadOnlyCollection<string> ModeratedNames => ModeratedNameList.ToList().AsReadOnly();

        private IEnumerable<RoomInfo> PublicList { get; }
        private IEnumerable<RoomInfo> PrivateList { get; }
        private IEnumerable<RoomInfo> OwnedList { get; }
        private IEnumerable<string> ModeratedNameList { get; }
    }
}