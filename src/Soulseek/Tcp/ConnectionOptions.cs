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

namespace Soulseek.Tcp
{
    /// <summary>
    ///     Options for connections.
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionOptions"/> class.
        /// </summary>
        /// <param name="readBufferSize">The read and write buffer size for underlying TCP connections.</param>
        /// <param name="connectTimeout">The connection timeout, in seconds, for client and peer TCP connections.</param>
        /// <param name="inactivityTimeout">The inactivity timeout, in seconds, for peer TCP connections.</param>
        public ConnectionOptions(int readBufferSize = 4096, int writeBufferSize = 4096, int connectTimeout = 5, int inactivityTimeout = 30)
        {
            ReadBufferSize = readBufferSize;
            WriteBufferSize = writeBufferSize;
            ConnectTimeout = connectTimeout;
            InactivityTimeout = inactivityTimeout;
        }

        /// <summary>
        ///     Gets the read and write buffer size for underlying TCP connections. (Default = 4096).
        /// </summary>
        public int ReadBufferSize { get; }

        /// <summary>
        ///     Gets the read and write buffer size for underlying TCP connections. (Default = 4096).
        /// </summary>
        public int WriteBufferSize { get; }

        /// <summary>
        ///     Gets the connection timeout, in seconds, for client and peer TCP connections.
        /// </summary>
        public int ConnectTimeout { get; }

        /// <summary>
        ///     Gets the inactivity timeout, in seconds, for peer TCP connections.
        /// </summary>
        /// <remarks>
        ///     Once connected and after reading data, if a no additional data is read within this threshold the connection will be
        ///     forcibly disconnected.
        /// </remarks>
        public int InactivityTimeout { get; }
    }
}