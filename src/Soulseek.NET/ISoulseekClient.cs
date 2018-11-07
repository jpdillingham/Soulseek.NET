// <copyright file="ISoulseekClient.cs" company="JP Dillingham">
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
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;

    public interface ISoulseekClient
    {
        #region Public Events

        /// <summary>
        ///     Occurs when a new browse response is received.
        /// </summary>
        event EventHandler<BrowseResponseReceivedEventArgs> BrowseResponseReceived;

        /// <summary>
        ///     Occurs when the underlying TCP connection to the server changes state.
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> ConnectionStateChanged;

        /// <summary>
        ///     Occurs when raw data is received by the underlying TCP connection.
        /// </summary>
        event EventHandler<DataReceivedEventArgs> DataReceived;

        /// <summary>
        ///     Occurs when a new message is received.
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     Occurs when a search is completed.
        /// </summary>
        event EventHandler<SearchCompletedEventArgs> SearchEnded;

        /// <summary>
        ///     Occurs when a new search result is received.
        /// </summary>
        event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        #endregion Public Events

        #region Public Properties

        /// <summary>
        ///     Gets or sets the address of the server to which to connect.
        /// </summary>
        string Address { get; set; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        ConnectionState ConnectionState { get; }

        /// <summary>
        ///     Gets the client options.
        /// </summary>
        SoulseekClientOptions Options { get; }

        /// <summary>
        ///     Gets information about peer connections.
        /// </summary>
        PeerInfo Peers { get; }

        /// <summary>
        ///     Gets or sets the port to which to connect.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        ///     Gets information about the connected server.
        /// </summary>
        ServerInfo Server { get; }

        #endregion Public Properties

        Task ConnectAsync();
        void Disconnect();
        void Dispose();

        Task<LoginResponse> LoginAsync(string username, string password);

        Task<Search> SearchAsync(string searchText, SearchOptions options = null, CancellationToken? cancellationToken = null);
        Task<Search> StartSearchAsync(string searchText, SearchOptions options = null);
        Task<Search> StopSearchAsync(Search search);

        Task BrowseAsync(string username, CancellationToken? cancellationToken);
    }
}