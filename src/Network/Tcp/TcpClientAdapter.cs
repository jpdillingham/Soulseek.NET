// <copyright file="TcpClientAdapter.cs" company="JP Dillingham">
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
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    ///     Provides client connections for TCP network services.
    /// </summary>
    /// <remarks>
    ///     This is a pass-through implementation of <see cref="ITcpClient"/> over <see cref="TcpClient"/> intended to enable
    ///     dependency injection.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal sealed class TcpClientAdapter : ITcpClient
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpClientAdapter"/> class with an optional <paramref name="tcpClient"/>.
        /// </summary>
        /// <param name="tcpClient">The optional TcpClient to wrap.</param>
        public TcpClientAdapter(TcpClient tcpClient = null)
        {
            TcpClient = tcpClient ?? new TcpClient();
        }

        /// <summary>
        ///     Gets the underlying <see cref="Socket"/>.
        /// </summary>
        public Socket Client => TcpClient.Client;

        /// <summary>
        ///     Gets a value indicating whether the client is connected.
        /// </summary>
        public bool Connected => TcpClient.Connected;

        /// <summary>
        ///     Gets the client remote endpoint.
        /// </summary>
        public IPEndPoint RemoteEndPoint => (IPEndPoint)TcpClient.Client.RemoteEndPoint;

        private bool Disposed { get; set; }
        private TcpClient TcpClient { get; set; }

        /// <summary>
        ///     Closes the client connection.
        /// </summary>
        public void Close()
        {
            TcpClient.Close();
            Dispose(false);
        }

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
        public Task ConnectAsync(IPAddress address, int port)
        {
            return TcpClient.ConnectAsync(address, port);
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="TcpClientAdapter"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Returns the <see cref="NetworkStream"/> used to send and receive data.
        /// </summary>
        /// <returns>The NetworkStream used to send and receive data.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the TCP client is not connected to a remote host.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the TCP client has been disposed.</exception>
        public INetworkStream GetStream()
        {
            return new NetworkStreamAdapter(TcpClient.GetStream());
        }

        public Stream GetRawStream() => TcpClient.GetStream();

        private void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    TcpClient.Dispose();
                }

                Disposed = true;
            }
        }
    }
}