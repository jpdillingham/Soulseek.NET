// <copyright file="ListenerHandler.cs" company="JP Dillingham">
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

namespace Soulseek.Network
{
    using System;
    using System.Linq;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;

    internal sealed class ListenerHandler : IListenerHandler
    {
        public ListenerHandler(
            SoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new ClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

        public async void HandleConnection(object sender, IConnection connection)
        {
            Diagnostic.Info($"Accepted incoming connection from {connection.IPAddress}:{SoulseekClient.Listener.Port}");

            try
            {
                var lengthBytes = await connection.ReadAsync(4).ConfigureAwait(false);
                var length = BitConverter.ToInt32(lengthBytes, 0);

                var bodyBytes = await connection.ReadAsync(length).ConfigureAwait(false);
                byte[] message = lengthBytes.Concat(bodyBytes).ToArray();

                if (PeerInitResponse.TryFromByteArray(message, out var peerInit))
                {
                    // this connection is the result of an unsolicited connection from the remote peer, either to request info or
                    // browse, or to send a file.
                    Diagnostic.Debug($"PeerInit for transfer type {peerInit.TransferType} received from {peerInit.Username} ({connection.IPAddress}:{SoulseekClient.Listener.Port})");

                    if (peerInit.TransferType == Constants.ConnectionType.Peer)
                    {
                        await SoulseekClient.PeerConnectionManager.AddMessageConnectionAsync(
                            peerInit.Username,
                            connection.HandoffTcpClient()).ConfigureAwait(false);
                    }
                    else if (peerInit.TransferType == Constants.ConnectionType.Tranfer)
                    {
                        await SoulseekClient.PeerConnectionManager.AddTransferConnectionAsync(
                            peerInit.Username,
                            peerInit.Token,
                            connection.HandoffTcpClient()).ConfigureAwait(false);
                    }
                    else if (peerInit.TransferType == Constants.ConnectionType.Distributed)
                    {
                        await SoulseekClient.DistributedConnectionManager.AddChildConnectionAsync(
                            peerInit.Username,
                            connection.HandoffTcpClient()).ConfigureAwait(false);
                    }
                }
                else if (PierceFirewallResponse.TryFromByteArray(message, out var pierceFirewall))
                {
                    // this connection is the result of a ConnectToPeer request sent to the user, and the incoming message will
                    // contain the token that was provided in the request. Ensure this token is among those expected, and use it to
                    // determine the username of the remote user.
                    if (SoulseekClient.PeerConnectionManager.PendingSolicitations.TryGetValue(pierceFirewall.Token, out var peerUsername))
                    {
                        Diagnostic.Debug($"Peer PierceFirewall with token {pierceFirewall.Token} received from {peerUsername} ({connection.IPAddress}:{SoulseekClient.Listener.Port})");
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SolicitedPeerConnection, peerUsername, pierceFirewall.Token), connection);
                    }
                    else if (SoulseekClient.DistributedConnectionManager.PendingSolicitations.TryGetValue(pierceFirewall.Token, out var distributedUsername))
                    {
                        Diagnostic.Debug($"Distributed PierceFirewall with token {pierceFirewall.Token} received from {distributedUsername} ({connection.IPAddress}:{SoulseekClient.Listener.Port})");
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SolicitedDistributedConnection, distributedUsername, pierceFirewall.Token), connection);
                    }
                    else
                    {
                        throw new ConnectionException($"Unknown PierceFirewall attempt with token {pierceFirewall.Token} from {connection.IPAddress}:{connection.Port}");
                    }
                }
                else
                {
                    throw new ConnectionException($"Unknown direct connection type from {connection.IPAddress}:{connection.Port}");
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Failed to initialize direct connection from {connection.IPAddress}:{connection.Port}: {ex.Message}");
                connection.Disconnect(ex.Message);
                connection.Dispose();
            }
        }
    }
}
