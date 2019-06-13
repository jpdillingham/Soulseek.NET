// <copyright file="Listener.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    ///     Listens for client connections for TCP network services.
    /// </summary>
    internal class Listener : IListener
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Listener"/> class.
        /// </summary>
        /// <param name="port">The port of the listener.</param>
        /// <param name="tcpListener">The optional TcpClient instance to use.</param>
        public Listener(int port, ITcpListener tcpListener = null)
        {
            Port = port;
            TcpListener = tcpListener ?? new TcpListenerAdapter(new TcpListener(IPAddress.Parse("0.0.0.0"), port));
        }

        /// <summary>
        ///     Occurs when a new connection is accepted.
        /// </summary>
        public event EventHandler<ConnectionAcceptedEventArgs> Accepted;

        /// <summary>
        ///     Gets a value indicating whether the listener is listening for connections.
        /// </summary>
        public bool Listening { get; private set; } = false;

        /// <summary>
        ///     Gets the port of the listener.
        /// </summary>
        public int Port { get; }

        private ITcpListener TcpListener { get; set; }

        /// <summary>
        ///     Starts the listener.
        /// </summary>
        public void Start()
        {
            TcpListener.Start();
            Listening = true;
            Task.Run(() => ListenContinuouslyAsync()).Forget();
        }

        /// <summary>
        ///     Stops the listener.
        /// </summary>
        public void Stop()
        {
            TcpListener.Stop();
            Listening = false;
        }

        private async Task ListenContinuouslyAsync()
        {
            while (Listening)
            {
                var client = await TcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
                Task.Run(() => InitializeConnectionAsync(client)).Forget();
            }
        }

        private async Task InitializeConnectionAsync(TcpClient client)
        {
            var endPoint = (IPEndPoint)client.Client.RemoteEndPoint;

            try
            {
                var lengthBytes = await ReadAsync(client, 5).ConfigureAwait(false);
                var length = BitConverter.ToInt32(lengthBytes, 0);
                var code = (int)lengthBytes.Skip(4).ToArray()[0];
                var bytesRemaining = length - 1;

                // peer init
                if (code == 1)
                {
                    var restBytes = await ReadAsync(client, bytesRemaining).ConfigureAwait(false);
                    var nameLen = BitConverter.ToInt32(restBytes, 0);
                    var name = Encoding.ASCII.GetString(restBytes.Skip(4).Take(nameLen).ToArray());
                    var typeLen = BitConverter.ToInt32(restBytes, 4 + nameLen);
                    var type = Encoding.ASCII.GetString(restBytes.Skip(4 + nameLen + 4).Take(typeLen).ToArray());
                    var token = BitConverter.ToInt32(restBytes, 4 + nameLen + 4 + typeLen);

                    Accepted?.Invoke(this, new ConnectionAcceptedEventArgs(new TcpClientAdapter(client), type, name, token));
                }
                else if (code == 0)
                {
                    // todo: handle pierce firewall
                }
            }
            catch (Exception ex)
            {
                client.Dispose();
                throw new ConnectionException($"Failed to initialize incoming connection from {endPoint.Address}: {ex.Message}", ex);
            }
        }

        private async Task<byte[]> ReadAsync(TcpClient tcpClient, int length)
        {
            var result = new List<byte>();

            var buffer = new byte[4096];
            var totalBytesRead = 0;

            while (totalBytesRead < length)
            {
                var bytesRemaining = length - totalBytesRead;
                var bytesToRead = bytesRemaining > buffer.Length ? buffer.Length : bytesRemaining;

                var bytesRead = await tcpClient.GetStream().ReadAsync(buffer, 0, bytesToRead).ConfigureAwait(false);

                if (bytesRead == 0)
                {
                    throw new ConnectionException($"Remote connection closed.");
                }

                totalBytesRead += bytesRead;
                var data = buffer.Take(bytesRead);
                result.AddRange(data);
            }

            return result.ToArray();
        }
    }
}