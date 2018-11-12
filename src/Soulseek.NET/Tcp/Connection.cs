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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    internal class Connection : IConnection, IDisposable
    {
        internal Connection(ConnectionType type, string address, int port, ConnectionOptions options = null, ITcpClient tcpClient = null)
        {
            Type = type;
            Address = address;
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

        public event EventHandler<ConnectionStateChangedEventArgs> StateChanged;

        public ConnectionOptions Options { get; protected set; }
        public string Address { get; protected set; }
        public IPAddress IPAddress { get; protected set; }
        public int Port { get; protected set; }
        public ConnectionState State { get; protected set; } = ConnectionState.Disconnected;
        public ConnectionType Type { get; protected set; }
        public object Context { get; protected set; }

        protected bool Disposed { get; set; } = false;
        protected SystemTimer InactivityTimer { get; set; }
        protected NetworkStream Stream { get; set; }
        protected ITcpClient TcpClient { get; set; }
        protected SystemTimer WatchdogTimer { get; set; }

        public async Task ConnectAsync()
        {
            if (State != ConnectionState.Disconnected)
            {
                throw new ConnectionStateException($"Invalid attempt to connect a connected or transitioning connection (current state: {State})");
            }

            IPAddress = GetIPAddress(Address);

            // create a new TCS to serve as the trigger which will throw when the CTS times out a TCS is basically a 'fake' task
            // that ends when the result is set programmatically
            var taskCompletionSource = new TaskCompletionSource<bool>();

            try
            {
                ChangeServerState(ConnectionState.Connecting, $"Connecting to {IPAddress}:{Port}");

                // create a new CTS with our desired timeout. when the timeout expires, the cancellation will fire
                using (var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(Options.ConnectionTimeout)))
                {
                    var task = TcpClient.ConnectAsync(IPAddress, Port);

                    // register the TCS with the CTS. when the cancellation fires (due to timeout), it will set the value of the
                    // TCS via the registered delegate, ending the 'fake' task
                    using (cancellationTokenSource.Token.Register(() => taskCompletionSource.TrySetResult(true)))
                    {
                        // wait for both the connection task and the cancellation. if the cancellation ends first, throw.
                        if (task != await Task.WhenAny(task, taskCompletionSource.Task))
                        {
                            throw new OperationCanceledException($"Operation timed out after {Options.ConnectionTimeout} seconds", cancellationTokenSource.Token);
                        }

                        if (task.Exception?.InnerException != null)
                        {
                            throw task.Exception.InnerException;
                        }
                    }
                }

                ChangeServerState(ConnectionState.Connected, $"Connected to {IPAddress}:{Port}");
            }
            catch (Exception ex)
            {
                ChangeServerState(ConnectionState.Disconnected, $"Connection Error: {ex.Message}");

                throw new ConnectionException($"Failed to connect to {IPAddress}:{Port}: {ex.Message}", ex);
            }

            Stream = TcpClient.GetStream();
            WatchdogTimer.Start();
        }

        public void Disconnect(string message = null)
        {
            if (State != ConnectionState.Disconnected && State != ConnectionState.Disconnecting)
            {
                ChangeServerState(ConnectionState.Disconnecting, message);

                InactivityTimer.Stop();
                WatchdogTimer.Stop();
                Stream.Close();
                TcpClient.Close();

                ChangeServerState(ConnectionState.Disconnected, message);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public async Task SendAsync(byte[] bytes, bool suppressCodeNormalization = false)
        {
            if (!TcpClient.Connected)
            {
                throw new ConnectionStateException($"The underlying TcpConnection is closed.");
            }

            if (State != ConnectionState.Connected)
            {
                throw new ConnectionStateException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException($"Invalid attempt to send empty data.", nameof(bytes));
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

        protected void ChangeServerState(ConnectionState state, string message)
        {
            State = state;

            StateChanged?.Invoke(this, new ConnectionStateChangedEventArgs()
            {
                Address = Address,
                IPAddress = IPAddress.ToString(),
                Port = Port,
                State = state,
                Message = message
            });
        }

        protected IPAddress GetIPAddress(string address)
        {
            if (IPAddress.TryParse(address, out IPAddress ip))
            {
                return ip;
            }
            else
            {
                var dns = Dns.GetHostEntry(address);

                if (!dns.AddressList.Any())
                {
                    throw new ConnectionException($"Unable to resolve hostname {address}.");
                }

                return dns.AddressList[0];
            }
        }

        public async Task<byte[]> ReadAsync(long count)
        {
            try
            {
                var intCount = (int)count;
                return await ReadAsync(intCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"adsfasfdsa");
                throw new NotImplementedException($"File sizes exceeding ~2gb are not yet supported.");
            }
        }

        public async Task<byte[]> ReadAsync(int count)
        {
            return await ReadAsync(Stream, count);
        }

        protected async Task<byte[]> ReadAsync(NetworkStream stream, int count)
        {
            var result = new List<byte>();

            var buffer = new byte[Options.BufferSize];
            var totalBytesRead = 0;

            while (totalBytesRead < count)
            {
                var bytesRemaining = count - totalBytesRead;
                var bytesToRead = bytesRemaining > buffer.Length ? buffer.Length : bytesRemaining;

                var bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead);

                if (bytesRead == 0)
                {
                    Disconnect($"Remote connection closed.");
                }

                totalBytesRead += bytesRead;
                result.AddRange(buffer.Take(bytesRead));
            }

            return result.ToArray();
        }
    }
}