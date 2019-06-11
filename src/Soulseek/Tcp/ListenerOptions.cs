// <copyright file="ListenerOptions.cs" company="JP Dillingham">
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
    ///     Options for the connection listener.
    /// </summary>
    public class ListenerOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ListenerOptions"/> class.
        /// </summary>
        /// <param name="enabled">A value indicating whether to listen for incoming connections.</param>
        /// <param name="port">The listening port.</param>
        public ListenerOptions(bool enabled = true, int port = 2234)
        {
            Enabled = enabled;
            Port = port;
        }

        /// <summary>
        ///     Gets a value indicating whether to listen for incoming connections. (Default = true).
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        ///     Gets the listening port. (Default = 2234).
        /// </summary>
        public int Port { get; }
    }
}
