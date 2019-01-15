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

namespace Soulseek.NET.Messaging.Tcp
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Tcp;

    internal sealed class MessageConnection : Connection, IMessageConnection
    {
        internal MessageConnection(MessageConnectionType type, string username, IPAddress ipAddress, int port, ConnectionOptions options = null, ITcpClient tcpClient = null)
            : this(type, ipAddress, port, options, tcpClient)
        {
            Username = username;
        }

        internal MessageConnection(MessageConnectionType type, IPAddress ipAddress, int port, ConnectionOptions options = null, ITcpClient tcpClient = null)
            : base(ipAddress, port, options, tcpClient)
        {
            Type = type;

            // circumvent the inactivity timer for server connections; this connection is expected to idle.
            if (Type == MessageConnectionType.Server)
            {
                InactivityTimer = null;
            }

            Connected += async (sender, e) =>
            {
                Task.Run(() => ReadContinuouslyAsync()).Forget();
                await SendDeferredMessages().ConfigureAwait(false);
            };
        }

        public event EventHandler<Message> MessageRead;

        public override ConnectionKey Key => new ConnectionKey(Username, IPAddress, Port, Type);
        public MessageConnectionType Type { get; private set; }
        public string Username { get; private set; } = string.Empty;
        private ConcurrentQueue<Message> DeferredMessages { get; } = new ConcurrentQueue<Message>();

        public async Task SendMessageAsync(Message message)
        {
            if (State == ConnectionState.Disconnecting || State == ConnectionState.Disconnected)
            {
                throw new InvalidOperationException($"Invalid attempt to send to a disconnected or disconnecting connection (current state: {State})");
            }

            if (State == ConnectionState.Pending || State == ConnectionState.Connecting)
            {
                DeferredMessages.Enqueue(message);
            }
            else if (State == ConnectionState.Connected)
            {
                var bytes = message.ToByteArray();

                NormalizeMessageCode(bytes, 0 - (int)Type);
                await WriteAsync(bytes).ConfigureAwait(false);
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
            InactivityTimer?.Reset();

            while (true)
            {
                var message = new List<byte>();

                var lengthBytes = await ReadAsync(4).ConfigureAwait(false);
                var length = BitConverter.ToInt32(lengthBytes, 0);
                message.AddRange(lengthBytes);

                var codeBytes = await ReadAsync(4).ConfigureAwait(false);
                message.AddRange(codeBytes);

                var payloadBytes = await ReadAsync(length - 4).ConfigureAwait(false);
                message.AddRange(payloadBytes);

                var messageBytes = message.ToArray();

                NormalizeMessageCode(messageBytes, (int)Type);

                Task.Run(() => MessageRead?.Invoke(this, new Message(messageBytes))).Forget();
                InactivityTimer?.Reset();
            }
        }

        private async Task SendDeferredMessages()
        {
            while (!DeferredMessages.IsEmpty)
            {
                if (DeferredMessages.TryDequeue(out var deferredMessage))
                {
                    await SendMessageAsync(deferredMessage).ConfigureAwait(false);
                }
            }
        }
    }
}