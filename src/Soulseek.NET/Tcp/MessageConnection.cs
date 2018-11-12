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
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;

    internal sealed class MessageConnection : Connection, IDisposable
    {
        internal MessageConnection(ConnectionType type, string address, int port, ConnectionOptions options = null, ITcpClient tcpClient = null)
            : base(type, address, port, options, tcpClient)
        {
            StateChanged += MessageConnection_StateChanged;
        }

        event EventHandler<DataReceivedEventArgs> DataReceived;
        event EventHandler<MessageReceivedEventArgs> MessageReceived;

        private void MessageConnection_StateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Task.Run(() => ReadContinuouslyAsync()).Forget();
        }

        public async Task SendAsync(Message message, bool suppressCodeNormalization = false)
        {
            if (TcpClient.Connected)
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

            try
            {
                while (true)
                {
                    if (Type == ConnectionType.Transfer)
                    {
                        Console.WriteLine($"Trying to read transfer bytes...");

                        var buffer = new byte[Options.BufferSize];
                        var bytesRead = await Stream.ReadAsync(buffer, 0, Options.BufferSize);

                        if (bytesRead == 0)
                        {
                            Console.WriteLine(Encoding.ASCII.GetString(fileBytes.ToArray()));
                            Disconnect($"Remote connection closed.");
                        }

                        Console.WriteLine($"{bytesRead} bytes read");
                        fileBytes.AddRange(buffer.Take(bytesRead));
                    }
                    else
                    {
                        var message = new List<byte>();

                        var lengthBytes = await ReadAsync(Stream, 4);
                        var length = BitConverter.ToInt32(lengthBytes, 0);
                        message.AddRange(lengthBytes);

                        var codeBytes = await ReadAsync(Stream, 4);
                        var code = BitConverter.ToInt32(codeBytes, 0);
                        message.AddRange(codeBytes);

                        var payloadBytes = await ReadAsync(Stream, length - 4);
                        message.AddRange(payloadBytes);

                        var messageBytes = message.ToArray();

                        NormalizeMessageCode(messageBytes, (int)Type);

                        Task.Run(() => DataReceived?.Invoke(this, new DataReceivedEventArgs()
                        {
                            Address = Address,
                            IPAddress = IPAddress.ToString(),
                            Port = Port,
                            Data = messageBytes,
                        })).Forget();

                        if (Type == ConnectionType.Peer)
                        {
                            InactivityTimer.Reset();
                        }
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