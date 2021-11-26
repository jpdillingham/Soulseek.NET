// <copyright file="ConnectionOptions.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System;
    using System.Net.Sockets;

    /// <summary>
    ///     Options for connections.
    /// </summary>
    public class ConnectionOptions
    {
        private readonly Action<Socket> defaultConfigureSocketAction =
            (s) => { };

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionOptions"/> class.
        /// </summary>
        /// <param name="readBufferSize">The read buffer size for underlying TCP connections.</param>
        /// <param name="writeBufferSize">The write buffer size for underlying TCP connections.</param>
        /// <param name="writeQueueSize">The size of the write queue for double buffered writes.</param>
        /// <param name="connectTimeout">The connection timeout, in milliseconds, for client and peer TCP connections.</param>
        /// <param name="writeTimeout">(Obsolete) The timeout, in milliseconds, for write operations.</param>
        /// <param name="inactivityTimeout">The inactivity timeout, in milliseconds, for peer TCP connections.</param>
        /// <param name="keepAlive">(Obsolete) A value indicating whether the connection should use TCP KeepAlives.</param>
        /// <param name="proxyOptions">Optional SOCKS 5 proxy configuration options.</param>
        /// <param name="configureSocketAction">
        ///     The delegate invoked during instantiation to configure the server Socket instance.
        /// </param>
        public ConnectionOptions(
            int readBufferSize = 16384,
            int writeBufferSize = 16384,
            int writeQueueSize = 250,
            int connectTimeout = 10000,
            int writeTimeout = 5000,
            int inactivityTimeout = 15000,
            bool keepAlive = false,
            ProxyOptions proxyOptions = null,
            Action<Socket> configureSocketAction = null)
        {
            ReadBufferSize = readBufferSize;
            WriteBufferSize = writeBufferSize;
            WriteQueueSize = writeQueueSize;

            ConnectTimeout = connectTimeout;
            WriteTimeout = writeTimeout;
            InactivityTimeout = inactivityTimeout;
            KeepAlive = keepAlive;

            ProxyOptions = proxyOptions;

            ConfigureSocketAction = configureSocketAction ?? defaultConfigureSocketAction;
        }

        /// <summary>
        ///     Gets the delegate invoked during instantiation to configure the server Socket instance.
        /// </summary>
        public Action<Socket> ConfigureSocketAction { get; }

        /// <summary>
        ///     Gets the connection timeout, in milliseconds, for client and peer TCP connections. (Default = 10000).
        /// </summary>
        public int ConnectTimeout { get; }

        /// <summary>
        ///     Gets the inactivity timeout, in milliseconds, for peer TCP connections. (Default = 15000).
        /// </summary>
        /// <remarks>
        ///     Once connected and after reading data, if a no additional data is read within this threshold the connection will
        ///     be forcibly disconnected.
        /// </remarks>
        public int InactivityTimeout { get; }

        /// <summary>
        ///     Gets a value indicating whether the connection should use TCP KeepAlives.  (Default = false).
        /// </summary>
        [Obsolete("Use ConfigureSocketAction() to set KeepAlive and the various TcpKeepAlive parameters.  This will be removed in 4.x")]
        public bool KeepAlive { get; }

        /// <summary>
        ///     Gets the optional SOCKS 5 proxy configuration options.
        /// </summary>
        public ProxyOptions ProxyOptions { get; }

        /// <summary>
        ///     Gets the read buffer size for underlying TCP connections. (Default = 16384).
        /// </summary>
        public int ReadBufferSize { get; }

        /// <summary>
        ///     Gets the write buffer size for underlying TCP connections. (Default = 16384).
        /// </summary>
        public int WriteBufferSize { get; }

        /// <summary>
        ///     Gets the size of the write queue for double buffered writes.  (Default = 250).
        /// </summary>
        public int WriteQueueSize { get; }

        /// <summary>
        ///     Gets the timeout, in milliseconds, for write operations.  (Default = 5000).
        /// </summary>
        [Obsolete("This will be removed in 4.x")]
        public int WriteTimeout { get; }

        /// <summary>
        ///     Returns a new instance with <see cref="InactivityTimeout"/> fixed to -1, disabling it.
        /// </summary>
        /// <returns>A new instance with InactivityTimeout disabled.</returns>
        public ConnectionOptions WithoutInactivityTimeout()
        {
            return new ConnectionOptions(
                readBufferSize: ReadBufferSize,
                writeBufferSize: WriteBufferSize,
                writeQueueSize: WriteQueueSize,
                connectTimeout: ConnectTimeout,
                writeTimeout: WriteTimeout,
                inactivityTimeout: -1,
                keepAlive: KeepAlive,
                proxyOptions: ProxyOptions,
                configureSocketAction: ConfigureSocketAction);
        }

        /// <summary>
        ///     Returns a new instance with <see cref="KeepAlive"/> fixed to true, enabling it.
        /// </summary>
        /// <returns>A new instance with KeepAlive enabled.</returns>
        public ConnectionOptions WithKeepAlive()
        {
            return new ConnectionOptions(
                readBufferSize: ReadBufferSize,
                writeBufferSize: WriteBufferSize,
                writeQueueSize: WriteQueueSize,
                connectTimeout: ConnectTimeout,
                writeTimeout: WriteTimeout,
                inactivityTimeout: InactivityTimeout,
                keepAlive: true,
                proxyOptions: ProxyOptions,
                configureSocketAction: ConfigureSocketAction);
        }
    }
}