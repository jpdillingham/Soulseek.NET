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
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    internal class Connection : IConnection, IDisposable
    {
        #region Internal Constructors

        internal Connection(IPAddress ipAddress, int port, ConnectionOptions options = null, ITcpClient tcpClient = null)
        {
            IPAddress = ipAddress;
            Port = port;
            Options = options ?? new ConnectionOptions();
            TcpClient = tcpClient ?? new TcpClientAdapter(new TcpClient());

            if (Options.ReadTimeout > 0)
            {
                InactivityTimer = new SystemTimer()
                {
                    Enabled = false,
                    AutoReset = false,
                    Interval = Options.ReadTimeout * 1000,
                };

                InactivityTimer.Elapsed += (sender, e) => Disconnect($"Read timeout of {Options.ReadTimeout} seconds was reached.");
            }

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

        #endregion Internal Constructors

        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;
        public event EventHandler Connected;
        public event EventHandler<string> Disconnected;
        public event EventHandler<ConnectionDataEventArgs> DataRead;

        #region Public Properties

        public object Context { get; set; }
        public IPAddress IPAddress { get; protected set; }
        public virtual ConnectionKey Key => new ConnectionKey() { IPAddress = IPAddress, Port = Port };
        public ConnectionOptions Options { get; protected set; }
        public int Port { get; protected set; }
        public ConnectionState State { get; protected set; } = ConnectionState.Pending;

        #endregion Public Properties

        #region Protected Properties

        protected bool Disposed { get; set; } = false;
        protected SystemTimer InactivityTimer { get; set; }
        protected NetworkStream Stream { get; set; }
        protected ITcpClient TcpClient { get; set; }
        protected SystemTimer WatchdogTimer { get; set; }

        #endregion Protected Properties

        #region Public Methods

        public async Task ConnectAsync()
        {
            if (State != ConnectionState.Pending && State != ConnectionState.Disconnected)
            {
                throw new InvalidOperationException($"Invalid attempt to connect a connected or transitioning connection (current state: {State})");
            }

            // create a new TCS to serve as the trigger which will throw when the CTS times out a TCS is basically a 'fake' task
            // that ends when the result is set programmatically
            var taskCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                ChangeState(ConnectionState.Connecting, $"Connecting to {IPAddress}:{Port}");

                // create a new CTS with our desired timeout. when the timeout expires, the cancellation will fire
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(Options.ConnectTimeout)))
                {
                    var task = TcpClient.ConnectAsync(IPAddress, Port);

                    // register the TCS with the CTS. when the cancellation fires (due to timeout), it will set the value of the
                    // TCS via the registered delegate, ending the 'fake' task
                    using (cancellationTokenSource.Token.Register(() => taskCompletionSource.TrySetResult(true)))
                    {
                        // wait for both the connection task and the cancellation. if the cancellation ends first, throw.
                        if (task != await Task.WhenAny(task, taskCompletionSource.Task))
                        {
                            throw new OperationCanceledException($"Operation timed out after {Options.ConnectTimeout} seconds", cancellationTokenSource.Token);
                        }

                        if (task.Exception?.InnerException != null)
                        {
                            throw task.Exception.InnerException;
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<byte[]> ReadAsync(long count)
        {
            try
            {
                var intCount = (int)count;
                return await ReadAsync(intCount);
            }
            catch (Exception)
            {
                throw new NotImplementedException($"File sizes exceeding ~2gb are not yet supported.");
            }
        }

        public async Task<byte[]> ReadAsync(int count)
        {
            var result = new List<byte>();

            var buffer = new byte[Options.BufferSize];
            var totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                var bytesRemaining = count - totalBytesRead;
                var bytesToRead = bytesRemaining > buffer.Length ? buffer.Length : bytesRemaining;

                var bytesRead = await Stream.ReadAsync(buffer, 0, bytesToRead);

                if (bytesRead == 0)
                {
                    Disconnect($"Remote connection closed.");
                }

                totalBytesRead += bytesRead;
                var data = buffer.Take(bytesRead);
                result.AddRange(data);

                DataRead?.Invoke(this, new ConnectionDataEventArgs(data.ToArray(), totalBytesRead, count));
            }

            return result.ToArray();
        }

        public async Task SendAsync(byte[] bytes)
        {
            if (!TcpClient.Connected)
            {
                throw new InvalidOperationException($"The underlying TcpConnection is closed.");
            }

            if (State != ConnectionState.Connected)
            {
                throw new InvalidOperationException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException($"Invalid attempt to send empty data.", nameof(bytes));
            }

            if (bytes.Length > Options.BufferSize)
            {
                throw new NotImplementedException($"Write payloads exceeding the configured buffer size are not yet supported.");
            }

            try
            {
                await Stream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                if (State != ConnectionState.Connected)
                {
                    Disconnect($"Write error: {ex.Message}");
                }

                throw new ConnectionWriteException($"Failed to write {bytes.Length} bytes to {IPAddress}:{Port}: {ex.Message}", ex);
            }
        }

        #endregion Public Methods

        #region Protected Methods

        protected void ChangeState(ConnectionState state, string message)
        {
            State = state;

            var eventArgs = new ConnectionStateChangedEventArgs(state, message);

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

        protected void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    InactivityTimer?.Dispose();
                    WatchdogTimer?.Dispose();
                    Stream?.Dispose();
                    TcpClient?.Dispose();
                }

                Disposed = true;
            }
        }

        #endregion Protected Methods
    }
}