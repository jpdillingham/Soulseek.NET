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
    using System.Net;
    using System.Net.Sockets;
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
        public event EventHandler<IConnection> Accepted;

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

                Task.Run(() =>
                {
                    var endPoint = (IPEndPoint)client.Client.RemoteEndPoint;
                    var eventArgs = new Connection(endPoint.Address, endPoint.Port, null, new TcpClientAdapter(client));
                    Accepted?.Invoke(this, eventArgs);
                }).Forget();
            }
        }
    }
}