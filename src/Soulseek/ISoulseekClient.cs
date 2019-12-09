// <copyright file="ISoulseekClient.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Exceptions;

    /// <summary>
    ///     A client for the Soulseek file sharing network.
    /// </summary>
    public interface ISoulseekClient : IDisposable, IDiagnosticGenerator
    {
        /// <summary>
        ///     Occurs when the client connects.
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        ///     Occurs when the client disconnects.
        /// </summary>
        event EventHandler<SoulseekClientDisconnectedEventArgs> Disconnected;

        /// <summary>
        ///     Occurs when the client is logged in.
        /// </summary>
        event EventHandler LoggedIn;

        /// <summary>
        ///     Occurs when a private message is received.
        /// </summary>
        event EventHandler<PrivateMessageEventArgs> PrivateMessageReceived;

        /// <summary>
        ///     Occurs when a user joins a chat room.
        /// </summary>
        event EventHandler<RoomJoinedEventArgs> RoomJoined;

        /// <summary>
        ///     Occurs when a user leaves a chat room.
        /// </summary>
        event EventHandler<RoomLeftEventArgs> RoomLeft;

        /// <summary>
        ///     Occurs when a chat room message is received.
        /// </summary>
        event EventHandler<RoomMessageEventArgs> RoomMessageReceived;

        /// <summary>
        ///     Occurs when a new search response is received.
        /// </summary>
        event EventHandler<SearchResponseReceivedEventArgs> SearchResponseReceived;

        /// <summary>
        ///     Occurs when a search changes state.
        /// </summary>
        event EventHandler<SearchStateChangedEventArgs> SearchStateChanged;

        /// <summary>
        ///     Occurs when the client changes state.
        /// </summary>
        event EventHandler<SoulseekClientStateChangedEventArgs> StateChanged;

        /// <summary>
        ///     Occurs when an active transfer sends or receives data.
        /// </summary>
        event EventHandler<TransferProgressUpdatedEventArgs> TransferProgressUpdated;

        /// <summary>
        ///     Occurs when a transfer changes state.
        /// </summary>
        event EventHandler<TransferStateChangedEventArgs> TransferStateChanged;

        /// <summary>
        ///     Occurs when a watched user's status changes.
        /// </summary>
        /// <remarks>Add a user to the server watch list with <see cref="AddUserAsync(string, CancellationToken?)"/>.</remarks>
        event EventHandler<UserStatusChangedEventArgs> UserStatusChanged;

        /// <summary>
        ///     Gets the unresolved server address.
        /// </summary>
        string Address { get; }

        /// <summary>
        ///     Gets the resolved server address.
        /// </summary>
        IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the client options.
        /// </summary>
        SoulseekClientOptions Options { get; }

        /// <summary>
        ///     Gets the server port.
        /// </summary>
        int Port { get; }

        /// <summary>
        ///     Gets the current state of the underlying TCP connection.
        /// </summary>
        SoulseekClientStates State { get; }

        /// <summary>
        ///     Gets the name of the currently signed in user.
        /// </summary>
        string Username { get; }

        /// <summary>
        ///     Asynchronously sends a private message acknowledgement for the specified <paramref name="privateMessageId"/>.
        /// </summary>
        /// <param name="privateMessageId">The unique id of the private message to acknowledge.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="privateMessageId"/> is less than zero.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="PrivateMessageException">Thrown when an exception is encountered during the operation.</exception>
        Task AcknowledgePrivateMessageAsync(int privateMessageId, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously adds the specified <paramref name="username"/> to the server watch list for the current session.
        /// </summary>
        /// <remarks>
        ///     Once a user is added the server will begin sending status updates for that user, which will generate
        ///     <see cref="UserStatusChanged"/> events.
        /// </remarks>
        /// <param name="username">The username of the user to add.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="AddUserException">Thrown when an exception is encountered during the operation.</exception>
        Task<UserData> AddUserAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the list of files shared by the specified <paramref name="username"/> with the optionally
        ///     specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <remarks>
        ///     By default, this operation will not time out locally, but rather will wait until the remote connection is broken.
        ///     If a local timeout is desired, specify an appropriate <see cref="CancellationToken"/>.
        /// </remarks>
        /// <param name="username">The user to browse.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the fetched list of files.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="BrowseException">Thrown when an exception is encountered during the operation.</exception>
        Task<IReadOnlyCollection<Directory>> BrowseAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously connects the client to the server specified in the <see cref="Address"/> and <see cref="Port"/> properties.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is already connected.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="ConnectionException">Thrown when an exception is encountered during the operation.</exception>
        Task ConnectAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Disconnects the client from the server.
        /// </summary>
        /// <param name="message">An optional message describing the reason the client is being disconnected.</param>
        void Disconnect(string message = null);

        /// <summary>
        ///     Asynchronously downloads the specified <paramref name="filename"/> from the specified <paramref name="username"/>
        ///     using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="filename">The file to download.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including a byte array containing the file contents.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when a download of the specified <paramref name="filename"/> from the specified <paramref name="username"/>
        ///     is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        Task<byte[]> DownloadAsync(string username, string filename, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously downloads the specified <paramref name="filename"/> from the specified <paramref name="username"/>
        ///     using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>
        ///     to the specified <paramref name="outputStream"/>.
        /// </summary>
        /// <param name="username">The user from which to download the file.</param>
        /// <param name="filename">The file to download.</param>
        /// <param name="outputStream">The stream to which to write the file contents.</param>
        /// <param name="token">The unique download token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including a byte array containing the file contents.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="outputStream"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the specified <paramref name="outputStream"/> is not writeable.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when a download of the specified <paramref name="filename"/> from the specified <paramref name="username"/>
        ///     is already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        Task DownloadAsync(string username, string filename, Stream outputStream, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the current place of the specified <paramref name="filename"/> in the queue of the
        ///     specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user whose queue to check.</param>
        /// <param name="filename">The file to check.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the current place of the file in the queue.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="TransferNotFoundException">Thrown when a corresponding download is not active.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="DownloadPlaceInQueueException">Thrown when an exception is encountered during the operation.</exception>
        Task<int> GetDownloadPlaceInQueueAsync(string username, string filename, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Gets the next token for use in client operations.
        /// </summary>
        /// <remarks>
        ///     <para>Tokens are returned sequentially and the token value rolls over to 0 when it has reached <see cref="int.MaxValue"/>.</para>
        ///     <para>This operation is thread safe.</para>
        /// </remarks>
        /// <returns>The next token.</returns>
        /// <threadsafety instance="true"/>
        int GetNextToken();

        /// <summary>
        ///     Asynchronously fetches the list of chat rooms on the server.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the list of server rooms.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="RoomListException">Thrown when an exception is encountered during the operation.</exception>
        Task<IReadOnlyCollection<Room>> GetRoomListAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the IP address and port of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the connection information.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the connection information.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserAddressException">Thrown when an exception is encountered during the operation.</exception>
        Task<(IPAddress IPAddress, int Port)> GetUserAddressAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches information about the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user from which to fetch the information.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the information response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserInfoException">Thrown when an exception is encountered during the operation.</exception>
        Task<UserInfo> GetUserInfoAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously fetches the status of the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The username of the user for which to fetch the status.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserStatusException">Thrown when an exception is encountered during the operation.</exception>
        Task<(UserStatus Status, bool IsPrivileged)> GetUserStatusAsync(string username, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously joins the chat room with the specified <paramref name="roomName"/>.
        /// </summary>
        /// <remarks>When successful, a corresponding <see cref="RoomJoined"/> event will be raised.</remarks>
        /// <param name="roomName">The name of the chat room to join.</param>
        /// <param name="cancellationToken">The token to minotor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="RoomJoinException">Thrown when an exception is encountered during the operation.</exception>
        Task<RoomData> JoinRoomAsync(string roomName, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously leaves the chat room with the specified <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The name of the chat room to leave.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the server response.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="RoomJoinException">Thrown when an exception is encountered during the operation.</exception>
        Task LeaveRoomAsync(string roomName, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously logs in to the server with the specified <paramref name="username"/> and <paramref name="password"/>.
        /// </summary>
        /// <param name="username">The username with which to log in.</param>
        /// <param name="password">The password with which to log in.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="password"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a user is already logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="LoginRejectedException">Thrown when the login is rejected by the remote server.</exception>
        /// <exception cref="LoginException">Thrown when an exception is encountered during the operation.</exception>
        Task LoginAsync(string username, string password, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously searches for the specified <paramref name="searchText"/> using the specified unique
        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The operation <see cref="SearchOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the search results.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the specified <paramref name="searchText"/> is null, empty, or consists of only whitespace.
        /// </exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SearchException">Thrown when an unhandled Exception is encountered during the operation.</exception>
        Task<IReadOnlyCollection<SearchResponse>> SearchAsync(string searchText, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously searches for the specified <paramref name="searchText"/> using the specified unique
        ///     <paramref name="token"/> and with the optionally specified <paramref name="options"/> and <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="searchText">The text for which to search.</param>
        /// <param name="responseReceived">The delegate to invoke for each response.</param>
        /// <param name="token">The unique search token.</param>
        /// <param name="options">The operation <see cref="SearchOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation, including the search results.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the specified <paramref name="searchText"/> is null, empty, or consists of only whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when the specified <paramref name="responseReceived"/> delegate is null.
        /// </exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SearchException">Thrown when an unhandled Exception is encountered during the operation.</exception>
        Task SearchAsync(string searchText, Action<SearchResponse> responseReceived, int? token = null, SearchOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously sends the specified private <paramref name="message"/> to the specified <paramref name="username"/>.
        /// </summary>
        /// <param name="username">The user to which the message is to be sent.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="message"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="PrivateMessageException">Thrown when an exception is encountered during the operation.</exception>
        Task SendPrivateMessageAsync(string username, string message, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously sends the specified chat room <paramref name="message"/> to the specified <paramref name="roomName"/>.
        /// </summary>
        /// <param name="roomName">The name of the room to which the message is to be sent.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="roomName"/> or <paramref name="message"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="RoomMessageException">Thrown when an exception is encountered during the operation.</exception>
        Task SendRoomMessageAsync(string roomName, string message, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously informs the server of the number of shared <paramref name="directories"/> and <paramref name="files"/>.
        /// </summary>
        /// <param name="directories">The number of shared directories.</param>
        /// <param name="files">The number of shared files.</param>
        /// <param name="cancellationToken">The token to monitor for cancelation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the value of <paramref name="directories"/> or <paramref name="files"/> is less than zero.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="SharedCountsException">Thrown when an exception is encountered during the operation.</exception>
        Task SetSharedCountsAsync(int directories, int files, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously informs the server of the current online <paramref name="status"/> of the client.
        /// </summary>
        /// <param name="status">The current status.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="UserStatusException">Thrown when an exception is encountered during the operation.</exception>
        Task SetUserStatusAsync(UserStatus status, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously uploads the specified <paramref name="filename"/> containing <paramref name="data"/> to the the
        ///     specified <paramref name="username"/> using the specified unique <paramref name="token"/> and optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to which to upload the file.</param>
        /// <param name="filename">The filename of the file to upload.</param>
        /// <param name="data">The file contents.</param>
        /// <param name="token">The unique upload token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when the specified <paramref name="data"/> is null or of zero length.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when an upload of the specified <paramref name="filename"/> to the specified <paramref name="username"/> is
        ///     already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        Task UploadAsync(string username, string filename, byte[] data, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously uploads the specified <paramref name="filename"/> from the specified <paramref name="inputStream"/>
        ///     to the the specified <paramref name="username"/> using the specified unique <paramref name="token"/> and
        ///     optionally specified <paramref name="cancellationToken"/>.
        /// </summary>
        /// <param name="username">The user to which to upload the file.</param>
        /// <param name="filename">The filename of the file to upload.</param>
        /// <param name="length">The size of the file to upload, in bytes.</param>
        /// <param name="inputStream">The stream from which to retrieve the file contents.</param>
        /// <param name="token">The unique upload token.</param>
        /// <param name="options">The operation <see cref="TransferOptions"/>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="username"/> or <paramref name="filename"/> is null, empty, or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="length"/> is less than 1.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="inputStream"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the specified <paramref name="inputStream"/> is not readable.
        /// </exception>
        /// <exception cref="InvalidOperationException">Thrown when the client is not connected or logged in.</exception>
        /// <exception cref="DuplicateTokenException">Thrown when the specified or generated token is already in use.</exception>
        /// <exception cref="DuplicateTransferException">
        ///     Thrown when an upload of the specified <paramref name="filename"/> to the specified <paramref name="username"/> is
        ///     already in progress.
        /// </exception>
        /// <exception cref="TimeoutException">Thrown when the operation has timed out.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation has been cancelled.</exception>
        /// <exception cref="TransferException">Thrown when an exception is encountered during the operation.</exception>
        Task UploadAsync(string username, string filename, long length, Stream inputStream, int? token = null, TransferOptions options = null, CancellationToken? cancellationToken = null);
    }
}