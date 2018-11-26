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
        ///     Gets or sets the message timeout, in seconds, used when waiting for a response from the server.  (Default = 5).
        /// </summary>
        public int MessageTimeout { get; set; } = 5;

        /// <summary>
        ///     Gets or sets the number of allowed concurrent peer connections.  (Default = 500).
        /// </summary>
        public int ConcurrentPeerConnections { get; set; } = 500;

        /// <summary>
        ///     Gets or sets a value indicating whether download progress events are invoked synchronously.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         If this option is not set, events may not be received in the proper order.
        ///     </para>
        ///     <para>
        ///         Enabling this option may impact download performance.
        ///     </para>
        /// </remarks>
        public bool UseSynchronousDownloadProgressEvents { get; set; } = false;

        /// <summary>
        ///     Gets or sets the options for the server message connection.
        /// </summary>
        public ConnectionOptions ConnectionOptions { get; set; } = new ConnectionOptions();

        /// <summary>
        ///     Gets or sets the options for peer message connections.
        /// </summary>
        public ConnectionOptions PeerConnectionOptions { get; set; } = new ConnectionOptions();

        /// <summary>
        ///     Gets or sets the options for peer transfer connections.
        /// </summary>
        public ConnectionOptions TransferConnectionOptions { get; set; } = new ConnectionOptions();
    }
}