// <copyright file="TcpClientAdapter.cs" company="JP Dillingham">
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
    using System.Diagnostics.CodeAnalysis;
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
    internal sealed class TcpClientAdapter : ITcpClient, IDisposable
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TcpClientAdapter"/> class.
        /// </summary>
        /// <param name="tcpClient">The optional TcpClient to wrap.</param>
        internal TcpClientAdapter(TcpClient tcpClient = null)
        {
            TcpClient = tcpClient ?? new TcpClient();
        }

        /// <summary>
        ///     Gets a value indicating whether the client is connected.
        /// </summary>
        public bool Connected => TcpClient.Connected;

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
        ///     Asynchronously connects to the specified <paramref name="ipAddress"/> and <paramref name="port"/>.
        /// </summary>
        /// <param name="ipAddress">The IP address to which to connect.</param>
        /// <param name="port">The port to which to connect.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task ConnectAsync(IPAddress ipAddress, int port)
        {
            await TcpClient.ConnectAsync(ipAddress, port).ConfigureAwait(false);
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
        public NetworkStream GetStream()
        {
            return TcpClient.GetStream();
        }

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