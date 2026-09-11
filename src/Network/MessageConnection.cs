// <copyright file="MessageConnection.cs" company="JP Dillingham">
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

namespace Soulseek.Network
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     Provides client connections to the Soulseek network.
    /// </summary>
    internal sealed class MessageConnection : Connection, IMessageConnection
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageConnection"/> class.
        /// </summary>
        /// <param name="username">The username of the peer associated with the connection, if applicable.</param>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="codeLength">The length message codes received, in bytes.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        internal MessageConnection(string username, IPEndPoint ipEndPoint, ConnectionOptions options = null, int codeLength = 4, ITcpClient tcpClient = null)
            : this(ipEndPoint, options, codeLength, tcpClient)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentException("The username must not be a null or empty string, or one consisting only of whitespace", nameof(username));
            }

            Username = username;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageConnection"/> class.
        /// </summary>
        /// <param name="ipEndPoint">The remote IP endpoint of the connection.</param>
        /// <param name="options">The optional options for the connection.</param>
        /// <param name="codeLength">The length message codes received, in bytes.</param>
        /// <param name="tcpClient">The optional TcpClient instance to use.</param>
        internal MessageConnection(IPEndPoint ipEndPoint, ConnectionOptions options = null, int codeLength = 4, ITcpClient tcpClient = null)
            : base(ipEndPoint, options, tcpClient)
        {
            CodeLength = codeLength;

            // bind the connected event to begin reading upon connection. if we received a connected client, this will never fire
            // and the read loop must be started via ReadContinuouslyAsync().
            Connected += (sender, e) =>
            {
                // if Username is empty, this is a server connection. begin reading continuously, and throw on exception.
                if (IsServerConnection)
                {
                    Task.Run(() => ReadContinuouslyAsync())
                        .ContinueWith(
                            continuationAction: t => throw new ConnectionException(t.Exception.Message, t.Exception),
                            continuationOptions: TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.RunContinuationsAsynchronously);
                }
                else
                {
                    // swallow exceptions from peer connections; these will be handled by timeouts.
                    Task.Run(() => ReadContinuouslyAsync()).Forget();
                }
            };
        }

        /// <summary>
        ///     Occurs when message data is received.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This event is separate from the underlying <see cref="Connection.DataRead"/> because it is bounded to the
        ///         message payload. The base event will be raised when reading the message length and code, while this event will not.
        ///     </para>
        ///     <para>
        ///         This event is only useful for tracking the progress of large messages (larger than the receive buffer);
        ///         basically only the response to a browse request. There is no corresponding event for data written, as this
        ///         library sends messages in their entirety, and the two would be fuctionally identical.
        ///     </para>
        /// </remarks>
        public event EventHandler<MessageDataEventArgs> MessageDataRead;

        /// <summary>
        ///     Occurs when a new message is read in its entirety.
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageRead;

        /// <summary>
        ///     Occurs when a new message is received, but before it is read.
        /// </summary>
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        /// <summary>
        ///     Occurs when a message is written in its entirety.
        /// </summary>
        public event EventHandler<MessageEventArgs> MessageWritten;

        /// <summary>
        ///     Gets the length message codes received, in bytes.
        /// </summary>
        public int CodeLength { get; }

        /// <summary>
        ///     Gets a value indicating whether this connection is connected to the server, as opposed to a peer.
        /// </summary>
        public bool IsServerConnection => string.IsNullOrEmpty(Username);

        /// <summary>
        ///     Gets the unique identifier for the connection.
        /// </summary>
        public override ConnectionKey Key => new ConnectionKey(Username, IPEndPoint);

        /// <summary>
        ///     Gets a value indicating whether the internal continuous read loop is running.
        /// </summary>
        public bool ReadingContinuously { get; private set; }

        /// <summary>
        ///     Gets the username of the peer associated with the connection, if applicable.
        /// </summary>
        public string Username { get; } = string.Empty;

        private ConcurrentDictionary<int, ConcurrentQueue<(Stream Stream, Action Callback)>> MessageHandlingOverrideRegistrations { get; } = new ConcurrentDictionary<int, ConcurrentQueue<(Stream Stream, Action Callback)>>();

        /// <summary>
        ///     Registers an override for handling of the specified <paramref name="messageCode"/>, which will divert the
        ///     received data packets to the specified <paramref name="stream"/> instead of the attached message handler,
        ///     and will invoke the specified <paramref name="callback"/> when the message has been fully recieved.
        /// </summary>
        /// <remarks>
        ///     Registrations are added to a FIFO queue internally, and messages will be streamed to handlers in the order
        ///     they are registered and received. There is no way to guarantee that the remote client will respond in
        ///     chronological order, so avoid using this for messages that are variable in this way (e.g. search responses).
        /// </remarks>
        /// <param name="messageCode">The message code of the message for which to override handling.</param>
        /// <param name="stream">The stream to write the message data to.</param>
        /// <param name="callback">The callback to invoke when the message has been fully received.</param>
        public void RegisterMessageHandlingOverride(int messageCode, Stream stream, Action callback)
        {
            MessageHandlingOverrideRegistrations.AddOrUpdate(
                key: messageCode,
                addValue: new ConcurrentQueue<(Stream Stream, Action Callback)>(new[] { (stream, callback) }),
                updateValueFactory: (k, v) =>
                {
                    v.Enqueue((stream, callback));
                    return v;
                });
        }

        /// <summary>
        ///     Begins the internal continuous read loop, if it has not yet started.
        /// </summary>
        public void StartReadingContinuously()
        {
            if (!ReadingContinuously)
            {
                Task.Run(() => ReadContinuouslyAsync()).Forget();
            }
        }

        /// <summary>
        ///     Asynchronously writes the specified <paramref name="message"/> to the connection.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown when the specified <paramref name="message"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the connection state is not <see cref="ConnectionState.Connected"/>, or when the underlying TcpClient
        ///     is not connected.
        /// </exception>
        /// <exception cref="MessageException">
        ///     Thrown when an error is encountered while converting the message to a byte array.
        /// </exception>
        /// <exception cref="ConnectionWriteException">Thrown when an unexpected error occurs.</exception>
        public Task WriteAsync(IOutgoingMessage message, CancellationToken? cancellationToken = null)
        {
            if (message == default)
            {
                throw new ArgumentException("The specified message is null", nameof(message));
            }

            byte[] bytes;

            try
            {
                bytes = message.ToByteArray();
            }
            catch (Exception ex)
            {
                throw new MessageException("Failed to convert the message to a byte array", ex);
            }

            return WriteMessageInternalAsync(bytes, cancellationToken ?? CancellationToken.None);
        }

        private async Task ReadContinuouslyAsync()
        {
            if (ReadingContinuously)
            {
                return;
            }

            ReadingContinuously = true;
            byte[] codeBytes = null;

            void RaiseMessageDataRead(object sender, ConnectionDataEventArgs e)
            {
                if (SoulseekClient.RaiseEventsAsynchronously)
                {
                    Task.Run(() =>
                    {
                        Interlocked.CompareExchange(ref MessageDataRead, null, null)?
                            .Invoke(this, new MessageDataEventArgs(codeBytes, e.CurrentLength, e.TotalLength));
                    }, CancellationToken.None).Forget();
                }
                else
                {
                    Interlocked.CompareExchange(ref MessageDataRead, null, null)?
                            .Invoke(this, new MessageDataEventArgs(codeBytes, e.CurrentLength, e.TotalLength));
                }
            }

            try
            {
                while (!Disposed)
                {
                    try
                    {
                        var message = new List<byte>();

                        var lengthBytes = await ReadAsync(4, CancellationToken.None).ConfigureAwait(false);
                        var length = BitConverter.ToInt32(lengthBytes, 0);
                        message.AddRange(lengthBytes);

                        codeBytes = await ReadAsync(CodeLength, CancellationToken.None).ConfigureAwait(false);
                        message.AddRange(codeBytes);

                        RaiseMessageDataRead(this, new ConnectionDataEventArgs(0, length - CodeLength));

                        Interlocked.CompareExchange(ref MessageReceived, null, null)?
                            .Invoke(this, new MessageReceivedEventArgs(length, codeBytes));

                        DataRead += RaiseMessageDataRead;

                        // if a message stream 'hook' has been installed via InstallMessageStreamHook, stream the remainder
                        // of the message to the provided stream. the caller will be notified that the read is complete
                        // via MessageRead -> PeerMessageHandler.HandleMessageRead -> regular message handling
                        // the caller must avoid trying to use the browse response, since it would have been streamed instead of passed
                        if (BitConverter.ToInt32(codeBytes) == (int)MessageCode.Peer.BrowseResponse
                            && MessageHandlingOverrideRegistrations.TryGetValue((int)MessageCode.Peer.BrowseResponse, out var queue)
                            && queue.TryDequeue(out var entry))
                        {
                            await ReadAsync(length - CodeLength, entry.Stream, cancellationToken: CancellationToken.None).ConfigureAwait(false);
                            entry.Callback();
                        }
                        else
                        {
                            var payloadBytes = await ReadAsync(length - CodeLength, CancellationToken.None).ConfigureAwait(false);
                            message.AddRange(payloadBytes);
                            var messageBytes = message.ToArray();

                            if (SoulseekClient.RaiseEventsAsynchronously)
                            {
                                Task.Run(() =>
                                {
                                    Interlocked.CompareExchange(ref MessageRead, null, null)?
                                        .Invoke(this, new MessageEventArgs(messageBytes));
                                }, CancellationToken.None).Forget();
                            }
                            else
                            {
                                Interlocked.CompareExchange(ref MessageRead, null, null)?
                                    .Invoke(this, new MessageEventArgs(messageBytes));
                            }
                        }
                    }
                    finally
                    {
                        DataRead -= RaiseMessageDataRead;
                    }
                }
            }
            finally
            {
                ReadingContinuously = false;
            }
        }

        private async Task WriteMessageInternalAsync(byte[] bytes, CancellationToken cancellationToken)
        {
            await WriteAsync(bytes, cancellationToken).ConfigureAwait(false);

            if (SoulseekClient.RaiseEventsAsynchronously)
            {
                Task.Run(() =>
                {
                    Interlocked.CompareExchange(ref MessageWritten, null, null)?
                        .Invoke(this, new MessageEventArgs(bytes));
                }, cancellationToken).Forget();
            }
            else
            {
                Interlocked.CompareExchange(ref MessageWritten, null, null)?
                    .Invoke(this, new MessageEventArgs(bytes));
            }
        }
    }
}