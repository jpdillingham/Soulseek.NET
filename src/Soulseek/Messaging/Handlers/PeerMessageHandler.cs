namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Linq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Messaging;

    internal sealed class PeerMessageHandler : IPeerMessageHandler
    {
        public PeerMessageHandler(
            ISoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = (SoulseekClient)soulseekClient;
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

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
                        var searchResponse = SearchResponseSlim.Parse(message);
                        if (SoulseekClient.Searches.TryGetValue(searchResponse.Token, out var search))
                        {
                            search.AddResponse(searchResponse);
                        }

                        break;

                    case MessageCode.Peer.BrowseResponse:
                        var browseWaitKey = new WaitKey(MessageCode.Peer.BrowseResponse, connection.Username);
                        try
                        {
                            SoulseekClient.Waiter.Complete(browseWaitKey, BrowseResponse.Parse(message));
                        }
                        catch (Exception ex)
                        {
                            SoulseekClient.Waiter.Throw(browseWaitKey, new MessageReadException("The peer returned an invalid browse response.", ex));
                            throw;
                        }

                        break;

                    case MessageCode.Peer.InfoResponse:
                        var infoResponse = UserInfoResponse.Parse(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.InfoResponse, connection.Username), infoResponse);
                        break;

                    case MessageCode.Peer.TransferResponse:
                        var transferResponse = TransferResponse.Parse(message);
                        Console.WriteLine($"Got response from {connection.Username}: {transferResponse.Token}");
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.TransferResponse, connection.Username, transferResponse.Token), transferResponse);
                        break;

                    case MessageCode.Peer.QueueDownload:
                        // the end state here is to wait until there's actually a free slot, then send this request to the peer to
                        // let them know we are ready to start the actual transfer.
                        var queueDownloadRequest = QueueDownloadRequest.Parse(message);
                        var (queueAllowed, queueRejectionMessage) = SoulseekClient.Resolvers.QueueDownloadResponse(connection.Username, connection.IPAddress, connection.Port, queueDownloadRequest.Filename);

                        if (!queueAllowed)
                        {
                            await connection.WriteAsync(new QueueFailedResponse(queueDownloadRequest.Filename, queueRejectionMessage)).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.Peer.TransferRequest:
                        var transferRequest = TransferRequest.Parse(message);

                        if (transferRequest.Direction == TransferDirection.Upload)
                        {
                            SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.TransferRequest, connection.Username, transferRequest.Filename), transferRequest);
                        }
                        else
                        {
                            var (transferAllowed, transferRejectionMessage) = SoulseekClient.Resolvers.QueueDownloadResponse(connection.Username, connection.IPAddress, connection.Port, transferRequest.Filename);

                            if (!transferAllowed)
                            {
                                await connection.WriteAsync(new TransferResponse(transferRequest.Token, transferRejectionMessage)).ConfigureAwait(false);
                                await connection.WriteAsync(new QueueFailedResponse(transferRequest.Filename, transferRejectionMessage)).ConfigureAwait(false);
                            }
                            else
                            {
                                await connection.WriteAsync(new TransferResponse(transferRequest.Token, "Queued.")).ConfigureAwait(false);
                            }
                        }

                        break;

                    case MessageCode.Peer.QueueFailed:
                        var queueFailedResponse = QueueFailedResponse.Parse(message);
                        SoulseekClient.Waiter.Throw(new WaitKey(MessageCode.Peer.TransferRequest, connection.Username, queueFailedResponse.Filename), new TransferRejectedException(queueFailedResponse.Message));
                        break;

                    case MessageCode.Peer.PlaceInQueueResponse:
                        var placeInQueueResponse = PeerPlaceInQueueResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Peer.PlaceInQueueResponse, connection.Username, placeInQueueResponse.Filename), placeInQueueResponse);
                        break;

                    case MessageCode.Peer.UploadFailed:
                        var uploadFailedResponse = PeerUploadFailedResponse.FromByteArray(message);
                        var msg = $"Download of {uploadFailedResponse.Filename} reported as failed by {connection.Username}.";

                        var download = SoulseekClient.Downloads.Values.FirstOrDefault(d => d.Username == connection.Username && d.Filename == uploadFailedResponse.Filename);
                        if (download != null)
                        {
                            SoulseekClient.Waiter.Throw(new WaitKey(MessageCode.Peer.TransferRequest, download.Username, download.Filename), new TransferException(msg));
                            SoulseekClient.Waiter.Throw(download.WaitKey, new TransferException(msg));
                        }

                        Diagnostic.Debug(msg);
                        break;

                    case MessageCode.Peer.BrowseRequest:
                        var browseResponse = SoulseekClient.Resolvers.BrowseResponse(connection.Username, connection.IPAddress, connection.Port);
                        await connection.WriteAsync(browseResponse).ConfigureAwait(false);
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
    }
}
