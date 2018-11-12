// <copyright file="MessageConnection.cs" company="JP Dillingham">
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
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;

    internal sealed class MessageConnection : Connection, IDisposable, IMessageConnection
    {
        internal MessageConnection(ConnectionType type, string address, int port, ConnectionOptions options = null, ITcpClient tcpClient = null)
            : base(type, address, port, options, tcpClient)
        {
            StateChanged += MessageConnection_StateChanged;
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        private void MessageConnection_StateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Connected)
            {
                Console.WriteLine($"Connected, beginning read loop");
                Task.Run(() => ReadContinuouslyAsync()).Forget();
            }
        }

        public async Task SendMessageAsync(Message message, bool suppressCodeNormalization = false)
        {
            if (!TcpClient.Connected)
            {
                throw new ConnectionStateException($"The underlying TcpConnection is closed.");
            }

            if (State != ConnectionState.Connected)
            {
                throw new ConnectionStateException($"Invalid attempt to send to a disconnected or transitioning connection (current state: {State})");
            }

            var bytes = message.ToByteArray();

            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException($"Invalid attempt to send empty data.", nameof(bytes));
            }

            try
            {
                if (!suppressCodeNormalization)
                {
                    NormalizeMessageCode(bytes, 0 - (int)Type);
                }

                await Stream.WriteAsync(bytes, 0, bytes.Length);

                Console.WriteLine($"Sent {bytes.Length} bytes");
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

        private void NormalizeMessageCode(byte[] messageBytes, int newCode)
        {
            var code = BitConverter.ToInt32(messageBytes, 4);
            var adjustedCode = BitConverter.GetBytes(code + newCode);

            Array.Copy(adjustedCode, 0, messageBytes, 4, 4);
        }

        private async Task ReadContinuouslyAsync()
        {
            if (Type == ConnectionType.Peer)
            {
                InactivityTimer.Reset();
            }

            void log(string s)
            {
                if (Type == ConnectionType.Server)
                {
                    Console.WriteLine(s);
                }
            }

            var fileBytes = new List<byte>();

            var networkEventArgs = new NetworkEventArgs()
            {
                Address = Address,
                IPAddress = IPAddress.ToString(),
                Port = Port,
            };

            try
            {
                while (true)
                {
                    var message = new List<byte>();

                    var lengthBytes = await ReadAsync(Stream, 4);
                    var length = BitConverter.ToInt32(lengthBytes, 0);
                    Console.WriteLine($"Read {length} bytes");
                    message.AddRange(lengthBytes);

                    var codeBytes = await ReadAsync(Stream, 4);
                    var code = BitConverter.ToInt32(codeBytes, 0);
                    message.AddRange(codeBytes);

                    var payloadBytes = await ReadAsync(Stream, length - 4);
                    message.AddRange(payloadBytes);

                    var messageBytes = message.ToArray();

                    NormalizeMessageCode(messageBytes, (int)Type);

                    Task.Run(() => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(networkEventArgs) { Message = new Message(messageBytes) })).Forget();

                    if (Type == ConnectionType.Peer)
                    {
                        InactivityTimer.Reset();
                    }
                }
            }
            catch (Exception ex)
            {
                if (State != ConnectionState.Connected)
                {
                    Disconnect($"Read error: {ex.Message}");
                }

                if (Type == ConnectionType.Server)
                {
                    log($"Read Error: {ex}");
                }
            }
        }
    }
}