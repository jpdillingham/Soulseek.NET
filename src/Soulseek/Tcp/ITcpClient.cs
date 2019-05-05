// <copyright file="ITcpClient.cs" company="JP Dillingham">
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
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    ///     Provides client connections for TCP network services.
    /// </summary>
    internal interface ITcpClient : IDisposable
    {
        /// <summary>
        ///     Gets a value indicating whether the underlying <see cref="Socket"/> for an <see cref="ITcpClient"/> is connected to
        ///     a remote host.
        /// </summary>
        bool Connected { get; }

        /// <summary>
        ///     Disposes this <see cref="ITcpClient"/> and requests that the underlying TCP connection be closed.
        /// </summary>
        void Close();

        /// <summary>
        ///     Connects the client to a remote TCP host using the specified IP address and port number as an asynchronous operation.
        /// </summary>
        /// <param name="address">The IP address to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the address parameter is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the port parameter is not between <see cref="IPEndPoint.MinPort"/> and <see cref="IPEndPoint.MaxPort"/>.
        /// </exception>
        /// <exception cref="SocketException">Thrown when an error occurs while accessing the socket.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the TCP client has been disposed.</exception>
        Task ConnectAsync(IPAddress address, int port);

        /// <summary>
        ///     Returns the <see cref="NetworkStream"/> used to send and receive data.
        /// </summary>
        /// <returns>The NetworkStream used to send and receive data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the TCP client is not connected to a remote host.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the TCP client has been disposed.</exception>
        INetworkStream GetStream();
    }
}