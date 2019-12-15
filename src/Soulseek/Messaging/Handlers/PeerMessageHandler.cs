// <copyright file="PeerMessageHandler.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;

    /// <summary>
    ///     Handles incoming messages from peer connections.
    /// </summary>
    internal sealed class PeerMessageHandler : IPeerMessageHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerMessageHandler"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public PeerMessageHandler(
            SoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new SoulseekClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Handles incoming messages.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="message">The message.</param>
        public async void HandleMessage(object sender, byte[] message)
        {
            var connection = (IMessageConnection)sender;
            var code = new MessageReader<MessageCode.Peer>(message).ReadCode();

            Diagnostic.Debug($"Peer message received: {code} from {connection.Username} ({connection.IPAddress}:{connection.Port})");

            try
            {
                switch (code)
                {
                    case MessageCode.Peer.SearchResponse:
                        var searchResponse = SearchResponseSlim.FromByteArray(message);
                        if (SoulseekClient.Searches.TryGetValue(searchResponse.Token, out var search))
                        {
                            search.TryAddResponse(searchResponse);
                        }

                        break;

                    case MessageCode.Peer.BrowseResponse:
                        var browseWaitKey = new WaitKey(MessageCode.Peer.BrowseResponse, connection.Username);
                        try
                        {
                            SoulseekClient.Waiter.Complete(browseWaitKey, BrowseResponse.FromByteArray(message));
                        }
                        catch (Exception ex)
                        {
                            SoulseekClient.Waiter.Throw(browseWaitKey, new MessageReadException("The peer returned an invalid browse response.", ex));
                            throw;
                        }

                        break;

                    case MessageCode.Peer.InfoRequest:
                        var outgoingInfo = await new SoulseekClientOptions()
                            .UserInfoResponseResolver(connection.Username, connection.IPAddress, connection.Port).ConfigureAwait(false);

                        try
                        {
                            outgoingInfo = await SoulseekClient.Options
                                .UserInfoResponseResolver(connection.Username, connection.IPAddress, connection.Port).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Failed to resolve UserInfoResponse: {ex.Message}", ex);
                        }

                        await connection.WriteAsync(outgoingInfo.ToByteArray()).ConfigureAwait(false);
                        break;

                    case MessageCode.Peer.BrowseRequest:
                        // make a default response
                        var browseResponse = await new SoulseekClientOptions()
                            .BrowseResponseResolver(connection.Username, connection.IPAddress, connection.Port).ConfigureAwait(false);

                        try
                        {
                            browseResponse = await SoulseekClient.Options.BrowseResponseResolver(connection.Username, connection.IPAddress, connection.Port).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            Diagnostic.Warning($"Failed to resolve BrowseResponse: {ex.Message}", ex);
                        }

                        var browseResponseMessage = new BrowseResponse(browseResponse.Count(), browseResponse);

                        await connection.WriteAsync(browseResponseMessage.ToByteArray()).ConfigureAwait(false);
                        break;

                    case MessageCode.Peer.InfoResponse:
                        var incomingInfo = UserInfoResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.InfoResponse, connection.Username), incomingInfo);
                        break;

                    case MessageCode.Peer.TransferResponse:
                        var transferResponse = TransferResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.TransferResponse, connection.Username, transferResponse.Token), transferResponse);
                        break;

                    case MessageCode.Peer.QueueDownload:
                        var queueDownloadRequest = EnqueueDownloadRequest.FromByteArray(message);

                        var (queueRejected, queueRejectionMessage) =
                            await TryEnqueueDownloadAsync(connection.Username, connection.IPAddress, connection.Port, queueDownloadRequest.Filename).ConfigureAwait(false);

                        if (queueRejected)
                        {
                            await connection.WriteAsync(new EnqueueFailedResponse(queueDownloadRequest.Filename, queueRejectionMessage).ToByteArray()).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.Peer.TransferRequest:
                        var transferRequest = TransferRequest.FromByteArray(message);

                        if (transferRequest.Direction == TransferDirection.Upload)
                        {
                            SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.TransferRequest, connection.Username, transferRequest.Filename), transferRequest);
                        }
                        else
                        {
                            var (transferRejected, transferRejectionMessage) = await TryEnqueueDownloadAsync(connection.Username, connection.IPAddress, connection.Port, transferRequest.Filename).ConfigureAwait(false);

                            if (transferRejected)
                            {
                                await connection.WriteAsync(new TransferResponse(transferRequest.Token, transferRejectionMessage).ToByteArray()).ConfigureAwait(false);
                                await connection.WriteAsync(new EnqueueFailedResponse(transferRequest.Filename, transferRejectionMessage).ToByteArray()).ConfigureAwait(false);
                            }
                            else
                            {
                                await connection.WriteAsync(new TransferResponse(transferRequest.Token, "Queued.").ToByteArray()).ConfigureAwait(false);
                            }
                        }

                        break;

                    case MessageCode.Peer.QueueFailed:
                        var queueFailedResponse = EnqueueFailedResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Throw(new WaitKey(MessageCode.Peer.TransferRequest, connection.Username, queueFailedResponse.Filename), new TransferRejectedException(queueFailedResponse.Message));
                        break;

                    case MessageCode.Peer.PlaceInQueueResponse:
                        var placeInQueueResponse = PlaceInQueueResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.PlaceInQueueResponse, connection.Username, placeInQueueResponse.Filename), placeInQueueResponse);
                        break;

                    case MessageCode.Peer.UploadFailed:
                        var uploadFailedResponse = UploadFailed.FromByteArray(message);
                        var msg = $"Download of {uploadFailedResponse.Filename} reported as failed by {connection.Username}.";

                        var download = SoulseekClient.Downloads.Values.FirstOrDefault(d => d.Username == connection.Username && d.Filename == uploadFailedResponse.Filename);
                        if (download != null)
                        {
                            SoulseekClient.Waiter.Throw(new WaitKey(MessageCode.Peer.TransferRequest, download.Username, download.Filename), new TransferException(msg));
                            SoulseekClient.Waiter.Throw(download.WaitKey, new TransferException(msg));
                        }

                        Diagnostic.Debug(msg);
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled peer message: {code} from {connection.Username} ({connection.IPAddress}:{connection.Port}); {message.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling peer message: {code} from {connection.Username} ({connection.IPAddress}:{connection.Port}); {ex.Message}", ex);
            }
        }

        private async Task<(bool Rejected, string RejectionMessage)> TryEnqueueDownloadAsync(string username, IPAddress ipAddress, int port, string filename)
        {
            bool rejected = false;
            string rejectionMessage = string.Empty;

            try
            {
                await SoulseekClient.Options
                    .EnqueueDownloadAction(username, ipAddress, port, filename).ConfigureAwait(false);
            }
            catch (EnqueueDownloadException ex)
            {
                // pass the exception message through to the remote user only if QueueDownloadException is thrown
                rejected = true;
                rejectionMessage = ex.Message;
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Failed to invoke QueueDownload action: {ex.Message}", ex);

                // if any other exception is thrown, return a generic message. do this to avoid exposing potentially sensitive
                // information that may be contained in the Exception message (filesystem details, etc.)
                rejected = true;
                rejectionMessage = "Enqueue failed due to internal error.";
            }

            return (rejected, rejectionMessage);
        }
    }
}