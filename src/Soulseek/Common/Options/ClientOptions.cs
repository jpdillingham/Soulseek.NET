﻿// <copyright file="ClientOptions.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Exceptions;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Options for SoulseekClient.
    /// </summary>
    public sealed class ClientOptions
    {
        private readonly Func<string, IPAddress, int, Task<BrowseResponse>> defaultBrowseResponse =
            (u, i, p) => Task.FromResult(new BrowseResponse(0, new List<Directory>()));

        private readonly Func<string, IPAddress, int, string, Task> defaultQueueDownloadAction =
            (u, i, p, f) => { return Task.CompletedTask; };

        private readonly Func<string, IPAddress, int, Task<UserInfoResponse>> defaultUserInfoResponse =
            (u, i, p) => Task.FromResult(new UserInfoResponse(string.Empty, 0, 0, false));

        /// <summary>
        ///     Initializes a new instance of the <see cref="ClientOptions"/> class.
        /// </summary>
        /// <param name="listenPort">The port on which to listen for incoming connections.</param>
        /// <param name="concurrentDistributedChildrenLimit">The number of allowed distributed children.</param>
        /// <param name="concurrentPeerMessageConnectionLimit">The number of allowed concurrent outgoing peer message connections.</param>
        /// <param name="messageTimeout">The message timeout, in seconds, used when waiting for a response from the server.</param>
        /// <param name="autoAcknowledgePrivateMessages">
        ///     A value indicating whether to automatically send a private message acknowledgement upon receipt.
        /// </param>
        /// <param name="minimumDiagnosticLevel">The minimum level of diagnostic messages to be generated by the client.</param>
        /// <param name="startingToken">The starting value for download and search tokens.</param>
        /// <param name="serverConnectionOptions">The options for the server message connection.</param>
        /// <param name="peerConnectionOptions">The options for peer message connections.</param>
        /// <param name="transferConnectionOptions">The options for peer transfer connections.</param>
        /// <param name="incomingConnectionOptions">The options for incoming connections.</param>
        /// <param name="distributedConnectionOptions">The options for distributed message connections.</param>
        /// <param name="searchResponseResolver">
        ///     The delegate used to resolve the <see cref="SearchResponse"/> for an incoming <see cref="SearchRequest"/>.
        /// </param>
        /// <param name="browseResponseResolver">
        ///     The delegate used to resolve the <see cref="BrowseResponse"/> for an incoming <see cref="BrowseRequest"/>.
        /// </param>
        /// <param name="userInfoResponseResolver">
        ///     The delegate used to resolve the <see cref="UserInfoResponse"/> for an incoming <see cref="UserInfoRequest"/>.
        /// </param>
        /// <param name="queueDownloadAction">The delegate invoked upon an receipt of an incoming <see cref="QueueDownloadRequest"/>.</param>
        /// <param name="placeInQueueResponseResolver">
        ///     The delegate used to resolve the <see cref="PlaceInQueueResponse"/> for an incoming request.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value supplied for <paramref name="concurrentDistributedChildrenLimit"/> is less than zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value supplied for <paramref name="concurrentPeerMessageConnectionLimit"/> is less than one.</exception>
        public ClientOptions(
            int? listenPort = null,
            int concurrentDistributedChildrenLimit = 100,
            int concurrentPeerMessageConnectionLimit = 500,
            int messageTimeout = 5,
            bool autoAcknowledgePrivateMessages = true,
            DiagnosticLevel minimumDiagnosticLevel = DiagnosticLevel.Info,
            int startingToken = 0,
            ConnectionOptions serverConnectionOptions = null,
            ConnectionOptions peerConnectionOptions = null,
            ConnectionOptions transferConnectionOptions = null,
            ConnectionOptions incomingConnectionOptions = null,
            ConnectionOptions distributedConnectionOptions = null,
            Func<string, int, string, Task<SearchResponse>> searchResponseResolver = null,
            Func<string, IPAddress, int, Task<BrowseResponse>> browseResponseResolver = null,
            Func<string, IPAddress, int, Task<UserInfoResponse>> userInfoResponseResolver = null,
            Func<string, IPAddress, int, string, Task> queueDownloadAction = null,
            Func<string, IPAddress, int, string, Task<int>> placeInQueueResponseResolver = null)
        {
            ListenPort = listenPort;

            ConcurrentDistributedChildrenLimit = concurrentDistributedChildrenLimit;

            if (ConcurrentDistributedChildrenLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrentDistributedChildrenLimit), "Must be greater than or equal to zero.");
            }

            ConcurrentPeerMessageConnectionLimit = concurrentPeerMessageConnectionLimit;

            if (ConcurrentPeerMessageConnectionLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(concurrentPeerMessageConnectionLimit), "Must be greater than zero.");
            }

            MessageTimeout = messageTimeout;
            AutoAcknowledgePrivateMessages = autoAcknowledgePrivateMessages;
            MinimumDiagnosticLevel = minimumDiagnosticLevel;
            StartingToken = startingToken;

            ServerConnectionOptions = serverConnectionOptions ?? new ConnectionOptions();
            PeerConnectionOptions = peerConnectionOptions ?? new ConnectionOptions();
            TransferConnectionOptions = transferConnectionOptions ?? new ConnectionOptions();
            IncomingConnectionOptions = incomingConnectionOptions ?? new ConnectionOptions();
            DistributedConnectionOptions = distributedConnectionOptions ?? new ConnectionOptions();

            SearchResponseResolver = searchResponseResolver;
            BrowseResponseResolver = browseResponseResolver ?? defaultBrowseResponse;
            UserInfoResponseResolver = userInfoResponseResolver ?? defaultUserInfoResponse;
            QueueDownloadAction = queueDownloadAction ?? defaultQueueDownloadAction;
            PlaceInQueueResponseResolver = placeInQueueResponseResolver;
        }

        /// <summary>
        ///     Gets a value indicating whether to automatically send a private message acknowledgement upon receipt. (Default = true).
        /// </summary>
        public bool AutoAcknowledgePrivateMessages { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="BrowseResponse"/> for an incoming request. (Default = a response
        ///     with no files or directories).
        /// </summary>
        public Func<string, IPAddress, int, Task<BrowseResponse>> BrowseResponseResolver { get; }

        /// <summary>
        ///     Gets the number of allowed distributed children. (Default = 100).
        /// </summary>
        public int ConcurrentDistributedChildrenLimit { get; }

        /// <summary>
        ///     Gets the number of allowed concurrent outgoing peer message connections. (Default = 1000).
        /// </summary>
        public int ConcurrentPeerMessageConnectionLimit { get; }

        /// <summary>
        ///     Gets the options for distributed message connections.
        /// </summary>
        public ConnectionOptions DistributedConnectionOptions { get; }

        /// <summary>
        ///     Gets the options for incoming connections.
        /// </summary>
        public ConnectionOptions IncomingConnectionOptions { get; }

        /// <summary>
        ///     Gets the port on which to listen for incoming connections. (Default = null; do not listen).
        /// </summary>
        public int? ListenPort { get; }

        /// <summary>
        ///     Gets the message timeout, in seconds, used when waiting for a response from the server or peer. (Default = 5).
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
        ///     Gets the delegate used to resolve the <see cref="PlaceInQueueResponse"/> for an incoming request.
        /// </summary>
        public Func<string, IPAddress, int, string, Task<int>> PlaceInQueueResponseResolver { get; }

        /// <summary>
        ///     Gets the delegate invoked upon an receipt of an incoming <see cref="QueueDownloadRequest"/>. (Default = do nothing).
        /// </summary>
        /// <remarks>
        ///     This delegate must throw an Exception to indicate a rejected download. If the thrown Exception is of type
        ///     <see cref="QueueDownloadException"/> the message will be sent to the client, otherwise a default message will be sent.
        /// </remarks>
        public Func<string, IPAddress, int, string, Task> QueueDownloadAction { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="SearchResponse"/> for an incoming request. (Default = do not respond).
        /// </summary>
        public Func<string, int, string, Task<SearchResponse>> SearchResponseResolver { get; }

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

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="UserInfoResponse"/> for an incoming request. (Default = a
        ///     blank/zeroed response).
        /// </summary>
        public Func<string, IPAddress, int, Task<UserInfoResponse>> UserInfoResponseResolver { get; }
    }
}