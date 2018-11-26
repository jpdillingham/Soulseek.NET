// <copyright file="ConnectionOptions.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tcp
{
    /// <summary>
    ///     Options for connections.
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        ///     Gets or sets the read and write buffer size for underlying TCP connections. (Default = 4096).
        /// </summary>
        public int BufferSize { get; set; } = 4096;

        /// <summary>
        ///     Gets or sets the connection timeout, in seconds, for client and peer TCP connections. (Default = 5).
        /// </summary>
        public int ConnectTimeout { get; set; } = 5;

        /// <summary>
        ///     Gets or sets the read timeout, in seconds, for peer TCP connections. (Default = 5).
        /// </summary>
        /// <remarks>
        ///     Once connected and after reading data, if a no additional data is read within this threshold the connection will be
        ///     forcibly disconnected.
        /// </remarks>
        public int ReadTimeout { get; set; } = 5;
    }
}