// <copyright file="SearchScope.cs" company="JP Dillingham">
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

    public abstract class SearchScope
    {
        public static DefaultSearchScope Default => new DefaultSearchScope();
        public static UserSearchScope User(params string[] usernames) => new UserSearchScope(usernames);
        public static RoomSearchScope Room(string roomName) => new RoomSearchScope(roomName);
    }

    public class DefaultSearchScope : SearchScope
    {
    }

    public class UserSearchScope : SearchScope
    {
        public UserSearchScope(params string[] usernames)
        {
            Usernames = usernames;
        }

        public IEnumerable<string> Usernames { get; }
    }

    public class RoomSearchScope : SearchScope
    {
        public RoomSearchScope(string roomName)
        {
            RoomName = roomName;
        }

        public string RoomName { get; }
    }
}
