// <copyright file="SoulseekClientOptions.cs" company="JP Dillingham">
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
    using Soulseek.NET.Tcp;

    /// <summary>
    ///     Options for SoulseekClient.
    /// </summary>
    public class SoulseekClientOptions
    {
        /// <summary>
        ///     Gets or sets the message timeout used when waiting for a response from the server.
        /// </summary>
        public int MessageTimeout { get; set; } = 5;

        /// <summary>
        ///     Gets or sets the number of allowed concurrent peer connections.
        /// </summary>
        public int ConcurrentPeerConnections { get; set; } = 500;

        /// <summary>
        ///     Gets or sets the number of allowed concurrent downloads.
        /// </summary>
        public int ConcurrentDownloads { get; set; } = 25;

        /// <summary>
        ///     Gets or sets the options for the underlying Tcp connection.
        /// </summary>
        public ConnectionOptions ConnectionOptions { get; set; } = new ConnectionOptions();
        public ConnectionOptions PeerConnectionOptions { get; set; } = new ConnectionOptions();
        public ConnectionOptions TransferConnectionOptions { get; set; } = new ConnectionOptions();
    }
}