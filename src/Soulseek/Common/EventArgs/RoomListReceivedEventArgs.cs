// <copyright file="RoomListReceivedEventArgs.cs" company="JP Dillingham">
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

    /// <summary>
    ///     Event arguments for events raised upon receipt of the list of rooms.
    /// </summary>
    public class RoomListReceivedEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomListReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="rooms">The list of rooms.</param>
        public RoomListReceivedEventArgs(IReadOnlyCollection<RoomInfo> rooms)
        {
            Rooms = rooms;
        }

        /// <summary>
        ///     Gets the list of rooms.
        /// </summary>
        public IReadOnlyCollection<RoomInfo> Rooms { get; }
    }
}