// <copyright file="PrivilegedUserList.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using Soulseek.Exceptions;

    /// <summary>
    ///     A list of the privileged users on the server.
    /// </summary>
    public static class PrivilegedUserList
    {
        /// <summary>
        ///     Parses a new instance of <see cref="PrivilegedUserList"/> from the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The message from which to parse.</param>
        /// <returns>The parsed instance.</returns>
        public static IReadOnlyCollection<string> Parse(byte[] message)
        {
            var reader = new MessageReader<MessageCode.Server>(message);
            var code = reader.ReadCode();

            if (code != MessageCode.Server.PrivilegedUsers)
            {
                throw new MessageException($"Message Code mismatch creating Privileged Users response (expected: {(int)MessageCode.Server.PrivilegedUsers}, received: {(int)code}");
            }

            var count = reader.ReadInteger();
            var list = new List<string>();

            for (int i = 0; i < count; i++)
            {
                list.Add(reader.ReadString());
            }

            return list.AsReadOnly();
        }
    }
}