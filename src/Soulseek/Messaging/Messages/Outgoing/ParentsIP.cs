// <copyright file="ParentsIP.cs" company="JP Dillingham">
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
    using System;
    using System.Net;

    /// <summary>
    ///     Informs the server of the IP address of the current distributed parent.
    /// </summary>
    internal sealed class ParentsIP
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ParentsIP"/> class.
        /// </summary>
        /// <param name="ipAddress">The IP address of the current distributed parent.</param>
        public ParentsIP(IPAddress ipAddress)
        {
            IPAddress = ipAddress;
        }

        /// <summary>
        ///     Gets the IP address of the current distributed parent.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            var ipBytes = IPAddress.GetAddressBytes();
            Array.Reverse(ipBytes);

            return new MessageBuilder()
                .WriteCode(MessageCode.Server.ParentsIP)
                .WriteBytes(ipBytes)
                .Build();
        }
    }
}