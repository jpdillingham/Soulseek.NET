// <copyright file="Browse.cs" company="JP Dillingham">
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

namespace Soulseek.NET
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;

    public sealed class Browse
    {
        internal Browse(string username, string ipAddress, int port, BrowseOptions options = null, CancellationToken? cancellationToken = null, IConnection connection = null)
        {
            Username = username;
            IPAddress = ipAddress;
            Port = port;

            Options = options ?? new BrowseOptions();
            CancellationToken = cancellationToken;

            Connection = connection ?? new Connection(ConnectionType.Peer, ipAddress, port, Options.ConnectionTimeout, Options.ReadTimeout, Options.BufferSize);
        }

        public string Username { get; private set; }
        public string IPAddress { get; private set; }
        public int Port { get; private set; }
        public BrowseResponse Response { get; private set; }

        public BrowseOptions Options { get; private set; }
        private IConnection Connection { get; set; }
        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();
        private CancellationToken? CancellationToken { get; set; }

        internal async Task<Browse> BrowseAsync()
        {
            Connection.DataReceived += OnConnectionDataReceived;
            Connection.StateChanged += OnConnectionStateChanged;

            try
            {
                await Connection.ConnectAsync();

                var token = new Random().Next();
                await Connection.SendAsync(new PeerInitRequest(Username, "P", token).ToByteArray(), suppressCodeNormalization: true);
                await Connection.SendAsync(new PeerBrowseRequest().ToByteArray());

                Response = await MessageWaiter.WaitIndefinitely<BrowseResponse>(MessageCode.PeerBrowseResponse, IPAddress, CancellationToken);
                return this;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to browse user {Username}.", ex);
            }
        }

        private void OnConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            var message = new Message(e.Data);

            switch (message.Code)
            {
                case MessageCode.PeerBrowseResponse:
                    MessageWaiter.Complete(MessageCode.PeerBrowseResponse, e.IPAddress, BrowseResponse.Parse(message));
                    break;

                default:
                    if (sender is Connection connection)
                    {
                        connection.Disconnect($"Unknown browse response from peer: {message.Code}");
                    }

                    break;
            }
        }

        private async void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Disconnected && sender is Connection connection)
            {
                connection.Dispose();
                MessageWaiter.Throw(MessageCode.PeerBrowseResponse, e.IPAddress, new ConnectionException(e.Message));
            }
        }
    }
}
