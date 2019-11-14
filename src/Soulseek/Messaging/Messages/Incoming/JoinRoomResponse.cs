// <copyright file="JoinRoomResponse.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    using System.Collections.Generic;
    using System.Linq;
    using Soulseek.Exceptions;

    /// <summary>
    ///     The response to request to join a chat room.
    /// </summary>
    public sealed class JoinRoomResponse
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="JoinRoomResponse"/> class.
        /// </summary>
        internal JoinRoomResponse(string roomName, int userCount, IEnumerable<(string Username, UserData Data)> userList, bool isPrivateRoom = false, string owner = null, int? operatorCount = null, IEnumerable<string> operatorList = null)
        {
            RoomName = roomName;
            UserCount = userCount;
            UserList = userList;
            IsPrivateRoom = isPrivateRoom;
            Owner = owner;
            OperatorCount = operatorCount;
            OperatorList = operatorList;
        }

        public bool IsPrivateRoom { get; }
        public int? OperatorCount { get; }
        public IReadOnlyCollection<string> Operators => OperatorList?.ToList().AsReadOnly();
        public string Owner { get; }
        public string RoomName { get; }
        public int UserCount { get; }
        public IReadOnlyCollection<(string Username, UserData Data)> Users => UserList?.ToList().AsReadOnly();
        private IEnumerable<string> OperatorList { get; }
        private IEnumerable<(string Username, UserData Data)> UserList { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="JoinRoomResponse"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static JoinRoomResponse FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Server>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.JoinRoom)
            {
                throw new MessageException($"Message Code mismatch creating Join Room response (expected: {(int)MessageCode.Server.JoinRoom}, received: {(int)code}");
            }

            var roomName = reader.ReadString();

            var userCount = reader.ReadInteger();
            var userNames = new List<string>();

            for (int i = 0; i < userCount; i++)
            {
                userNames.Add(reader.ReadString());
            }

            var statusCount = reader.ReadInteger();
            var statuses = new List<UserStatus>();

            for (int i = 0; i < statusCount; i++)
            {
                statuses.Add((UserStatus)reader.ReadInteger());
            }

            var dataCount = reader.ReadInteger();
            var datums = new List<(int AverageSpeed, long DownloadCount, int FileCount, int DirectoryCount)>();

            for (int i = 0; i < dataCount; i++)
            {
                var averageSpeed = reader.ReadInteger();
                var downloadCount = reader.ReadLong();
                var fileCount = reader.ReadInteger();
                var directoryCount = reader.ReadInteger();

                datums.Add((averageSpeed, downloadCount, fileCount, directoryCount));
            }

            var slotsFreeCount = reader.ReadInteger();
            var slots = new List<int>();

            for (int i = 0; i < slotsFreeCount; i++)
            {
                slots.Add(reader.ReadInteger());
            }

            var countryCount = reader.ReadInteger();
            var countries = new List<string>();

            for (int i = 0; i < countryCount; i++)
            {
                countries.Add(reader.ReadString());
            }

            var users = new List<(string Username, UserData Data)>();

            for (int i = 0; i < userCount; i++)
            {
                var name = userNames[i];
                var status = statuses[i];
                var (averageSpeed, downloadCount, fileCount, directoryCount) = datums[i];
                var slot = slots[i];
                var country = countries[i];

                users.Add((name, new UserData(status, averageSpeed, downloadCount, fileCount, directoryCount, slot, country)));
            }

            string owner = null;
            int? operatorCount = null;
            List<string> operatorList = null;

            if (reader.HasMoreData)
            {
                owner = reader.ReadString();
                operatorCount = reader.ReadInteger();
                operatorList = new List<string>();

                for (int i = 0; i < operatorCount; i++)
                {
                    operatorList.Add(reader.ReadString());
                }
            }

            return new JoinRoomResponse(roomName, userCount, users, owner != null, owner, operatorCount, operatorList);
        }
    }
}