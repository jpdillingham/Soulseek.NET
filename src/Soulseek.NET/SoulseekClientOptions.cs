﻿// <copyright file="SoulseekClientOptions.cs" company="JP Dillingham">
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
    public sealed class SoulseekClientOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientOptions"/> class.
        /// </summary>
        /// <param name="concurrentPeerConnections">The number of allowed concurrent peer message connections.</param>
        /// <param name="messageTimeout">The message timeout, in seconds, used when waiting for a response from the server.</param>
        /// <param name="autoAcknowledgePrivateMessages">
        ///     A value indicating whether to automatically send a private message acknowledgement upon receipt.
        /// </param>
        /// <param name="minimumDiagnosticLevel">The minimum level of diagnostic messages to be generated by the client.</param>
        /// <param name="startingToken">The starting value for download and search tokens.</param>
        /// <param name="serverConnectionOptions">The options for the server message connection.</param>
        /// <param name="peerConnectionOptions">The options for peer message connections.</param>
        /// <param name="transferConnectionOptions">The options for peer transfer connections.</param>
        public SoulseekClientOptions(
            int concurrentPeerConnections = 500,
            int messageTimeout = 5,
            bool autoAcknowledgePrivateMessages = true,
            DiagnosticLevel minimumDiagnosticLevel = DiagnosticLevel.Info,
            int startingToken = 0,
            ConnectionOptions serverConnectionOptions = null,
            ConnectionOptions peerConnectionOptions = null,
            ConnectionOptions transferConnectionOptions = null)
        {
            ConcurrentPeerConnections = concurrentPeerConnections;
            MessageTimeout = messageTimeout;
            AutoAcknowledgePrivateMessages = autoAcknowledgePrivateMessages;
            MinimumDiagnosticLevel = minimumDiagnosticLevel;
            StartingToken = startingToken;
            ServerConnectionOptions = serverConnectionOptions ?? new ConnectionOptions();
            PeerConnectionOptions = peerConnectionOptions ?? new ConnectionOptions();
            TransferConnectionOptions = transferConnectionOptions ?? new ConnectionOptions();
        }

        /// <summary>
        ///     Gets a value indicating whether to automatically send a private message acknowledgement upon receipt. (Default = true).
        /// </summary>
        public bool AutoAcknowledgePrivateMessages { get; }

        /// <summary>
        ///     Gets the number of allowed concurrent peer message connections. (Default = 500).
        /// </summary>
        public int ConcurrentPeerConnections { get; }

        /// <summary>
        ///     Gets the message timeout, in seconds, used when waiting for a response from the server. (Default = 5).
        /// </summary>
        public int MessageTimeout { get; }

        /// <summary>
        ///     Gets the minimum level of diagnostic messages to be generated by the client. (Default = None).
        /// </summary>
        public DiagnosticLevel MinimumDiagnosticLevel { get; }

        /// <summary>
        ///     Gets the options for peer message connections.
        /// </summary>
        public ConnectionOptions PeerConnectionOptions { get; }

        /// <summary>
        ///     Gets the options for the server message connection.
        /// </summary>
        public ConnectionOptions ServerConnectionOptions { get; }

        /// <summary>
        ///     Gets the starting value for download and search tokens. (Default = 0).
        /// </summary>
        public int StartingToken { get; }

        /// <summary>
        ///     Gets the options for peer transfer connections.
        /// </summary>
        public ConnectionOptions TransferConnectionOptions { get; }
    }
}