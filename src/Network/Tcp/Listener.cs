// <copyright file="Listener.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, version 3.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
//
//     This program is distributed with Additional Terms pursuant to Section 7
//     of the GPLv3.  See the LICENSE file in the root directory of this
//     project for the complete terms and conditions.
//
//     SPDX-FileCopyrightText: JP Dillingham
//     SPDX-License-Identifier: GPL-3.0-only
// </copyright>

namespace Soulseek.Network.Tcp
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    /// <summary>
    ///     Listens for client connections for TCP network services.
    /// </summary>
    /// <remarks>
    ///     Excluded from code coverage due to the inability to test the accepted code block; You can't instantiate TcpClient with
    ///     an ip and port without it connecting immediately, so the test either must create a new connection to *something*, or a
    ///     bunch of hoops need to be jumped through to handle TcpClients coming from the listener not connected/without an
    ///     endpoint, both of which will and SHOULD throw exceptions and die.
    /// </remarks>
    [ExcludeFromCodeCoverage]
    internal sealed class Listener : IListener
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Listener"/> class.
        /// </summary>
        /// <param name="ipAddress">The IP address to which to bind the listener.</param>
        /// <param name="port">The port of the listener.</param>
        /// <param name="connectionOptions">The optional options to use when creating <see cref="IConnection"/> instances.</param>
        /// <param name="tcpListener">The optional TcpClient instance to use.</param>
        public Listener(IPAddress ipAddress, int port, ConnectionOptions connectionOptions, ITcpListener tcpListener = null)
        {
            IPAddress = ipAddress;
            Port = port;
            ConnectionOptions = connectionOptions ?? new ConnectionOptions();
            TcpListener = tcpListener ?? new TcpListenerAdapter(new TcpListener(ipAddress, port));
        }

        /// <summary>
        ///     Occurs when a new connection is accepted.
        /// </summary>
        public event EventHandler<IConnection> Accepted;

        /// <summary>
        ///     Gets the options used when creating new <see cref="IConnection"/> instances.
        /// </summary>
        public ConnectionOptions ConnectionOptions { get; }

        /// <summary>
        ///     Gets the port of the listener.
        /// </summary>
        public IPAddress IPAddress { get; }

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
                    var eventArgs = new Connection(endPoint, ConnectionOptions, new TcpClientAdapter(client));
                    Accepted?.Invoke(this, eventArgs);
                }).Forget();
            }
        }
    }
}
