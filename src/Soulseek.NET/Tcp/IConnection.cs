// <copyright file="IConnection.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tcp
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

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
        ///     Occurs when the connection is disconnected.
        /// </summary>
        event EventHandler<string> Disconnected;

        /// <summary>
        ///     Occurs when the connection state changes.
        /// </summary>
        event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        /// <summary>
        ///     Gets or sets the generic connection context.
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
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task ConnectAsync();

        /// <summary>
        ///     Disconnects the client.
        /// </summary>
        /// <param name="message">The optional message or reason for the disconnect.</param>
        void Disconnect(string message = null);

        /// <summary>
        ///     Asynchronously reads the specified number of bytes from the connection.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The read bytes.</returns>
        Task<byte[]> ReadAsync(int count);

        /// <summary>
        ///     Asynchronously reads the specified number of bytes from the connection.
        /// </summary>
        /// <param name="count">The number of bytes to read.</param>
        /// <returns>The read bytes.</returns>
        Task<byte[]> ReadAsync(long count);

        /// <summary>
        ///     Asynchronously writes the specified bytes to the connection.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        Task WriteAsync(byte[] bytes);
    }
}