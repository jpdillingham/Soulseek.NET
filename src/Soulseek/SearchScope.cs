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
    using Soulseek.Messaging.Messages;

    public abstract class SearchScope
    {
        public static DefaultSearchScope Default => new DefaultSearchScope();
        public static UserSearchScope User(params string[] usernames) => new UserSearchScope(usernames);
        public static RoomSearchScope Room(string roomName) => new RoomSearchScope(roomName);

        public abstract byte[] CreateSearchRequestMessage(string searchText, int token);
    }

    public class DefaultSearchScope : SearchScope
    {
        public override byte[] CreateSearchRequestMessage(string searchText, int token)
        {
            return new SearchRequest(searchText, token).ToByteArray();
        }
    }

    public class UserSearchScope : SearchScope
    {
        public UserSearchScope(params string[] usernames)
        {
            Usernames = usernames;
        }

        public override byte[] CreateSearchRequestMessage(string searchText, int token)
        {
            var message = new List<byte>();

            foreach (var username in Usernames)
            {
                message.AddRange(new UserSearchRequest(username, searchText, token).ToByteArray());
            }

            return message.ToArray();
        }

        public IEnumerable<string> Usernames { get; }
    }

    public class RoomSearchScope : SearchScope
    {
        public RoomSearchScope(string roomName)
        {
            RoomName = roomName;
        }

        public override byte[] CreateSearchRequestMessage(string searchText, int token)
        {
            return new RoomSearchRequest(RoomName, searchText, token).ToByteArray();
        }

        public string RoomName { get; }
    }
}
