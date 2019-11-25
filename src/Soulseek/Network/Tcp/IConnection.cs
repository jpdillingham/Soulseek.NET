// <copyright file="IConnection.cs" company="JP Dillingham">
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

namespace Soulseek.Network.Tcp
{
    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Exceptions;

    /// <summary>
    ///     Provides client connections for TCP network services.
    /// </summary>
    internal interface IConnection : IDisposable
    {
        /// <summary>
        ///     Occurs when the connection is connected.
        /// </summary>
        event EventHandler Connected;

        /// <summary>
        ///     Occurs when data is ready from the connection.
        /// </summary>
        event EventHandler<ConnectionDataEventArgs> DataRead;

        /// <summary>
        ///     Occurs when data has been written to the connection.
        /// </summary>
        event EventHandler<ConnectionDataEventArgs> DataWritten;

        /// <summary>
        ///     Occurs when the connection is disconnected.
        /// </summary>
        event EventHandler<string> Disconnected;

        /// <summary>
        ///     Occurs when the connection state changes.
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        /// <summary>
        ///     Gets or sets the connection context.
        /// </summary>
        object Context { get; set; }

        /// <summary>
        ///     Gets the remote IP address of the connection.
        /// </summary>
        IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the unique identifier of the connection.
        /// </summary>
        ConnectionKey Key { get; }

        /// <summary>
        ///     Gets the options for the connection.
        /// </summary>
        ConnectionOptions Options { get; }

        /// <summary>
        ///     Gets the remote port of the connection.
        /// </summary>
        int Port { get; }

        /// <summary>
        ///     Gets the current connection state.
        /// </summary>
        ConnectionState State { get; }

        /// <summary>
        ///     Asynchronously connects the client to the configured <see cref="IPAddress"/> and <see cref="Port"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection is already connected, or is transitioning between states.
        /// </exception>
        /// <exception cref="TimeoutException">
        ///     Thrown when the time attempting to connect exceeds the configured <see cref="ConnectionOptions.ConnectTimeout"/> value.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        ///     Thrown when <paramref name="cancellationToken"/> cancellation is requested.
        /// </exception>
        /// <exception cref="ConnectionException">Thrown when an unexpected error occurs.</exception>
        Task ConnectAsync(CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Disconnects the client.
        /// </summary>
        /// <param name="message">The optional message or reason for the disconnect.</param>
        void Disconnect(string message = null);

        /// <summary>
        ///     Decouples and returns the underlying TCP connection for this connection, allowing the TCP connection to survive
        ///     beyond the lifespan of this instance.
        /// </summary>
        /// <returns>The underlying TCP connection for this connection.</returns>
        ITcpClient HandoffTcpClient();

        /// <summary>
        ///     Asynchronously reads the specified number of bytes from the connection.
        /// </summary>
        /// <remarks>The connection is disconnected if a <see cref="ConnectionReadException"/> is thrown.</remarks>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation, including the read bytes.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="length"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="ConnectionReadException">Thrown when an unexpected error occurs.</exception>
        Task<byte[]> ReadAsync(long length, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously reads the specified number of bytes from the connection.
        /// </summary>
        /// <remarks>The connection is disconnected if a <see cref="ConnectionReadException"/> is thrown.</remarks>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="governor">The delegate used to govern transfer speed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation, including the read bytes.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="length"/> is less than 1.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="ConnectionReadException">Thrown when an unexpected error occurs.</exception>
        Task<byte[]> ReadAsync(long length, Func<Task> governor, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously writes the specified bytes to the connection.
        /// </summary>
        /// <remarks>The connection is disconnected if a <see cref="ConnectionWriteException"/> is thrown.</remarks>
        /// <param name="bytes">The bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="bytes"/> array is null or empty.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="ConnectionWriteException">Thrown when an unexpected error occurs.</exception>
        Task WriteAsync(byte[] bytes, CancellationToken? cancellationToken = null);

        /// <summary>
        ///     Asynchronously writes the specified bytes to the connection.
        /// </summary>
        /// <remarks>The connection is disconnected if a <see cref="ConnectionWriteException"/> is thrown.</remarks>
        /// <param name="bytes">The bytes to write.</param>
        /// <param name="governor">The delegate used to govern transfer speed.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="bytes"/> array is null or empty.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="ConnectionWriteException">Thrown when an unexpected error occurs.</exception>
        Task WriteAsync(byte[] bytes, Func<Task> governor, CancellationToken? cancellationToken = null);
    }
}