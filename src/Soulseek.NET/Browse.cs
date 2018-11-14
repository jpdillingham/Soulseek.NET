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
        internal Browse(string username, string ipAddress, int port, BrowseOptions options, IMessageConnection connection = null)
        {
            Username = username;
            IPAddress = ipAddress;
            Port = port;

            Options = options;
            Connection = connection ?? new MessageConnection(ConnectionType.Peer, ipAddress, port, Options.ConnectionOptions);
        }

        public string Username { get; private set; }
        public string IPAddress { get; private set; }
        public int Port { get; private set; }
        public BrowseResponse Response { get; private set; }

        public BrowseOptions Options { get; private set; }
        private IMessageConnection Connection { get; set; }
        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();

        internal async Task<Browse> BrowseAsync(CancellationToken? cancellationToken = null)
        {
            Connection.MessageReceived += OnConnectionMessageReceived;
            Connection.StateChanged += OnConnectionStateChanged;

            try
            {
                await Connection.ConnectAsync();

                var token = new Random().Next();
                await Connection.SendAsync(new PeerInitRequest(Username, "P", token).ToMessage(), suppressCodeNormalization: true);
                await Connection.SendAsync(new PeerBrowseRequest().ToMessage());

                Response = await MessageWaiter.WaitIndefinitely<BrowseResponse>(MessageCode.PeerBrowseResponse, IPAddress, cancellationToken);
                return this;
            }
            catch (Exception ex)
            {
                throw new BrowseException($"Failed to browse user {Username}.", ex);
            }
        }

        private void OnConnectionMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine($"[BROWSE MESSAGE]: {e.Message.Code}");

            switch (e.Message.Code)
            {
                case MessageCode.PeerBrowseResponse:
                    MessageWaiter.Complete(MessageCode.PeerBrowseResponse, e.IPAddress.ToString(), BrowseResponse.Parse(e.Message));
                    break;

                default:
                    if (sender is Connection connection)
                    {
                        connection.Disconnect($"Unknown browse response from peer: {e.Message.Code}");
                    }

                    break;
            }
        }

        private void OnConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Disconnected && sender is Connection connection)
            {
                connection.Dispose();
                MessageWaiter.Throw(MessageCode.PeerBrowseResponse, e.IPAddress, new ConnectionException(e.Message));
            }
        }
    }
}
