// <copyright file="NetInfo.cs" company="JP Dillingham">
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
    using System;
    using System.Collections.Generic;
    using System.Net;
    using Soulseek.Exceptions;

    public sealed class NetInfo
    {
        internal NetInfo(int parentCount, IEnumerable<(string Username, IPAddress IPAddress, int Port)> parents)
        {
            ParentCount = parentCount;
            Parents = parents;
        }

        public int ParentCount { get; }
        public IEnumerable<(string Username, IPAddress IPAddress, int Port)> Parents { get; }

        public static NetInfo Parse(byte[] message)
        {
            var reader = new MessageReader<MessageCode>(message);
            var code = reader.ReadCode();

            if (code != MessageCode.ServerNetInfo)
            {
                throw new MessageException($"Message Code mismatch creating Net Info (expected: {(int)MessageCode.ServerGetStatus}, received: {(int)code}.");
            }

            var parentCount = reader.ReadInteger();
            var parents = new List<(string Username, IPAddress IPAddress, int Port)>();

            for (int i = 0; i < parentCount; i++)
            {
                var username = reader.ReadString();

                var ipBytes = reader.ReadBytes(4);
                Array.Reverse(ipBytes);
                var ipAddress = new IPAddress(ipBytes);

                var port = reader.ReadInteger();

                parents.Add((username, ipAddress, port));
            }

            return new NetInfo(parentCount, parents);
        }
    }
}
