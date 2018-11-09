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

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public interface ISoulseekClient
    {
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
        ///     Occurs when a new search result is received.
        /// </summary>
        event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        /// <summary>
        ///     Gets or sets the address of the server to which to connect.
        /// </summary>
        string Address { get; set; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        ConnectionState ConnectionState { get; }

        /// <summary>
        ///     Gets a value indicating whether a user is currently signed in.
        /// </summary>
        bool LoggedIn { get; }

        /// <summary>
        ///     Gets the client options.
        /// </summary>
        SoulseekClientOptions Options { get; }

        /// <summary>
        ///     Gets or sets the port to which to connect.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        ///     Gets information about the connected server.
        /// </summary>
        ServerInfo Server { get; }

        /// <summary>
        ///     Gets the name of the currently signed in user.
        /// </summary>
        string Username { get; }


        Task<SharesResponse> BrowseAsync(string username, BrowseOptions options = null, CancellationToken? cancellationToken = null);

        Task ConnectAsync();

        void Disconnect();

        void Dispose();

        Task<LoginResponse> LoginAsync(string username, string password);

        Task<Search> SearchAsync(string searchText, SearchOptions options = null, CancellationToken? cancellationToken = null);
    }
}