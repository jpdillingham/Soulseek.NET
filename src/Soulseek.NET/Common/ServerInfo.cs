// <copyright file="ServerInfo.cs" company="JP Dillingham">
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
    using System.Collections.Generic;

    public class ServerInfo
    {
        internal ServerInfo()
        {
        }

        public int ParentMinSpeed { get; internal set; }
        public int ParentSpeedRatio { get; internal set; }
        public IEnumerable<string> PrivilegedUsers { get; internal set; }
        public IEnumerable<Room> Rooms { get; internal set; }
        public int WishlistInterval { get; internal set; }
    }
}