// <copyright file="Connection.cs" company="JP Dillingham">
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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     Provides client connections for TCP network services.
    /// </summary>
    internal class Connection : IConnection
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Connection"/> class.
        /// </summary>
        /// <param name="ipAddress">The remote IP address of the connection.</param>
        /// <param name="port">The remote port of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        internal Connection(IPAddress ipAddress, int port, ConnectionOptions options = null, ITcpClient tcpClient = null)
        {
            IPAddress = ipAddress;
            Port = port;
            Options = options ?? new ConnectionOptions();
            TcpClient = tcpClient ?? new TcpClientAdapter(new TcpClient());

            InactivityTimer = new SystemTimer()
            {
                Enabled = false,
                AutoReset = false,
                Interval = Options.ReadTimeout * 1000,
            };

            InactivityTimer.Elapsed += (sender, e) => Disconnect($"Read timeout of {Options.ReadTimeout} seconds was reached.");

            WatchdogTimer = new SystemTimer()
            {
                Enabled = false,
                AutoReset = true,
                Interval = 250,
            };

            WatchdogTimer.Elapsed += (sender, e) =>
            {
                if (!TcpClient.Connected)
                {
                    Disconnect($"The server connection was closed unexpectedly.");
                }
            };
        }

        /// <summary>
        ///     Occurs when the connection is connected.
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        ///     Occurs when data is ready from the connection.
        /// </summary>
        public event EventHandler<ConnectionDataEventArgs> DataRead;

        /// <summary>
        ///     Occurs when the connection is disconnected.
        /// </summary>
        public event EventHandler<string> Disconnected;

        /// <summary>
        ///     Occurs when the connection state changes.
        /// </summary>
        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        /// <summary>
        ///     Gets or sets the generic connection context.
        /// </summary>
        public object Context { get; set; }

        /// <summary>
        ///     Gets or sets the remote IP address of the connection.
        /// </summary>
        public IPAddress IPAddress { get; protected set; }

        /// <summary>
        ///     Gets the unique identifier of the connection.
        /// </summary>
        public virtual ConnectionKey Key => new ConnectionKey(IPAddress, Port);

        /// <summary>
        ///     Gets or sets the options for the connection.
        /// </summary>
        public ConnectionOptions Options { get; protected set; }

        /// <summary>
        ///     Gets or sets the remote port of the connection.
        /// </summary>
        public int Port { get; protected set; }

        /// <summary>
        ///     Gets or sets the current connection state.
        /// </summary>
        public ConnectionState State { get; protected set; } = ConnectionState.Pending;

        /// <summary>
        ///     Gets or sets a value indicating whether the object is disposed.
        /// </summary>
        protected bool Disposed { get; set; } = false;

        /// <summary>
        ///     Gets or sets the timer used to monitor for transfer inactivity.
        /// </summary>
        protected SystemTimer InactivityTimer { get; set; }

        /// <summary>
        ///     Gets or sets the network stream for the connection.
        /// </summary>
        protected INetworkStream Stream { get; set; }

        /// <summary>
        ///     Gets or sets the TcpClient used by the connection.
        /// </summary>
        protected ITcpClient TcpClient { get; set; }

        /// <summary>
        ///     Gets or sets the timer used to monitor the status of the TcpClient.
        /// </summary>
        protected SystemTimer WatchdogTimer { get; set; }

        /// <summary>
        ///     Asynchronously connects the client to the configured <see cref="IPAddress"/> and <see cref="Port"/>.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection is already connected, or is transitioning between states.
        /// </exception>
        /// <exception cref="ConnectionException">Thrown when an unexpected error occurs.</exception>
        public async Task ConnectAsync(CancellationToken? cancellationToken = null)
        {
            if (State != ConnectionState.Pending && State != ConnectionState.Disconnected)
            {
                throw new InvalidOperationException($"Invalid attempt to connect a connected or transitioning connection (current state: {State})");
            }

            cancellationToken = cancellationToken ?? CancellationToken.None;

            // create a new TCS to serve as the trigger which will throw when the CTS times out a TCS is basically a 'fake' task
            // that ends when the result is set programmatically.  create another for cancellation via the externally provided token.
            var timeoutTaskCompletionSource = new TaskCompletionSource<bool>();
            var cancellationTaskCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                ChangeState(ConnectionState.Connecting, $"Connecting to {IPAddress}:{Port}");

                // create a new CTS with our desired timeout. when the timeout expires, the cancellation will fire
                using (var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(Options.ConnectTimeout)))
                {
                    var connectTask = TcpClient.ConnectAsync(IPAddress, Port);

                    // register the TCS with the CTS. when the cancellation fires (due to timeout), it will set the value of the
                    // TCS via the registered delegate, ending the 'fake' task, then bind the externally supplied CT with the same TCS.
                    // either the timeout or the external token can now cancel the operation.
                    using (timeoutCancellationTokenSource.Token.Register(() => timeoutTaskCompletionSource.TrySetResult(true)))
                    using (((CancellationToken)cancellationToken).Register(() => cancellationTaskCompletionSource.TrySetResult(true)))
                    {
                        var completedTask = await Task.WhenAny(connectTask, timeoutTaskCompletionSource.Task, cancellationTaskCompletionSource.Task).ConfigureAwait(false);

                        if (completedTask == timeoutTaskCompletionSource.Task)
                        {
                            throw new TimeoutException($"Operation timed out after {Options.ConnectTimeout} seconds");
                        }
                        else if (completedTask == cancellationTaskCompletionSource.Task)
                        {
                            throw new OperationCanceledException("Operation cancelled.");
                        }

                        if (connectTask.Exception?.InnerException != null)
                        {
                            throw connectTask.Exception.InnerException;
                        }
                    }
                }

                WatchdogTimer.Start();
                Stream = TcpClient.GetStream();

                ChangeState(ConnectionState.Connected, $"Connected to {IPAddress}:{Port}");
            }
            catch (Exception ex)
            {
                ChangeState(ConnectionState.Disconnected, $"Connection Error: {ex.Message}");

                throw new ConnectionException($"Failed to connect to {IPAddress}:{Port}: {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Disconnects the client.
        /// </summary>
        /// <param name="message">The optional message or reason for the disconnect.</param>
        public void Disconnect(string message = null)
        {
            if (State != ConnectionState.Disconnected && State != ConnectionState.Disconnecting)
            {
                ChangeState(ConnectionState.Disconnecting, message);

                InactivityTimer?.Stop();
                WatchdogTimer?.Stop();
                Stream?.Close();
                TcpClient?.Close();

                ChangeState(ConnectionState.Disconnected, message);
            }
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="IConnection"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Asynchronously reads the specified number of bytes from the connection.
        /// </summary>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The read bytes.</returns>
        public async Task<byte[]> ReadAsync(long length, CancellationToken? cancellationToken = null)
        {
            // NetworkStream.ReadAsync doesn't support long, so if we were to support this we'd need to split the long up into
            // int-sized chunks and iterate. that's for later, if ever.
            if (!int.TryParse(length.ToString(CultureInfo.InvariantCulture), out var intLength))
            {
                throw new NotImplementedException($"File sizes exceeding ~2gb are not yet supported.");
            }

            return await ReadAsync(intLength, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     Asynchronously reads the specified number of bytes from the connection.
        /// </summary>
        /// <param name="length">The number of bytes to read.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The read bytes.</returns>
        public Task<byte[]> ReadAsync(int length, CancellationToken? cancellationToken = null)
        {
            if (length < 0)
            {
                throw new ArgumentException($"The requested length must be greater than or equal to zero.");
            }

            if (!TcpClient.Connected)
            {
                throw new InvalidOperationException($"The underlying Tcp connection is closed.");
            }

            if (State != ConnectionState.Connected)
            {
                throw new InvalidOperationException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            return ReadInternalAsync(length, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Asynchronously writes the specified bytes to the connection.
        /// </summary>
        /// <param name="bytes">The bytes to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public Task WriteAsync(byte[] bytes, CancellationToken? cancellationToken = null)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException($"Invalid attempt to send empty data.", nameof(bytes));
            }

            if (!TcpClient.Connected)
            {
                throw new InvalidOperationException($"The underlying Tcp connection is closed.");
            }

            if (State != ConnectionState.Connected)
            {
                throw new InvalidOperationException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            return WriteInternalAsync(bytes, cancellationToken ?? CancellationToken.None);
        }

        /// <summary>
        ///     Changes the state of the connection to the specified <paramref name="state"/> and raises events with the optionally
        ///     specified <paramref name="message"/>.
        /// </summary>
        /// <param name="state">The state to which to change.</param>
        /// <param name="message">The optional message describing the nature of the change.</param>
        protected void ChangeState(ConnectionState state, string message)
        {
            var eventArgs = new ConnectionStateChangedEventArgs(previousState: State, currentState: state, message: message);

            State = state;

            StateChanged?.Invoke(this, eventArgs);

            if (State == ConnectionState.Connected)
            {
                Connected?.Invoke(this, EventArgs.Empty);
            }
            else if (State == ConnectionState.Disconnected)
            {
                Disconnected?.Invoke(this, message);
            }
        }

        /// <summary>
        ///     Releases the managed and unmanaged resources used by the <see cref="Connection"/>.
        /// </summary>
        /// <param name="disposing">A value indicating whether the object is in the process of disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Disconnect();
                    InactivityTimer?.Dispose();
                    WatchdogTimer?.Dispose();
                    Stream?.Dispose();
                    TcpClient?.Dispose();
                }

                Disposed = true;
            }
        }

        private async Task<byte[]> ReadInternalAsync(int length, CancellationToken cancellationToken)
        {
            InactivityTimer?.Reset();

            var result = new List<byte>();

            var buffer = new byte[Options.BufferSize];
            var totalBytesRead = 0;

            try
            {
                while (totalBytesRead < length)
                {
                    var bytesRemaining = length - totalBytesRead;
                    var bytesToRead = bytesRemaining > buffer.Length ? buffer.Length : bytesRemaining;

                    var bytesRead = await Stream.ReadAsync(buffer, 0, bytesToRead, cancellationToken).ConfigureAwait(false);

                    if (bytesRead == 0)
                    {
                        Disconnect($"Remote connection closed.");
                        break;
                    }

                    totalBytesRead += bytesRead;
                    var data = buffer.Take(bytesRead);
                    result.AddRange(data);

                    DataRead?.Invoke(this, new ConnectionDataEventArgs(data.ToArray(), totalBytesRead, length));
                    InactivityTimer?.Reset();
                }

                return result.ToArray();
            }
            catch (Exception ex)
            {
                throw new ConnectionReadException($"Failed to read {length} bytes from {IPAddress}:{Port}: {ex.Message}", ex);
            }
        }

        private async Task WriteInternalAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            try
            {
                await Stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                InactivityTimer?.Reset();
            }
            catch (Exception ex)
            {
                throw new ConnectionWriteException($"Failed to write {bytes.Length} bytes to {IPAddress}:{Port}: {ex.Message}", ex);
            }
        }
    }
}