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
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    internal sealed class TcpClientAdapter : ITcpClient, IDisposable
    {
        internal TcpClientAdapter(TcpClient tcpClient = null)
        {
            TcpClient = tcpClient ?? new TcpClient();
        }

        public bool Connected => TcpClient.Connected;

        private bool Disposed { get; set; }
        private TcpClient TcpClient { get; set; }

        public void Close()
        {
            TcpClient.Close();
        }

        public async Task ConnectAsync(IPAddress ipAddress, int port)
        {
            await TcpClient.ConnectAsync(ipAddress, port);
        }

        public void Dispose()
        {
            Dispose(true);
        }

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