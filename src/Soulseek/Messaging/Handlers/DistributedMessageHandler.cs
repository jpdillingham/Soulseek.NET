namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Linq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Messaging.Tcp;

    internal sealed class DistributedMessageHandler : IDistributedMessageHandler
    {
        public DistributedMessageHandler(
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

        public async void HandleMessage(object sender, Message message)
        {
            var connection = (IMessageConnection)sender;

            var bytes = message.ToByteArray();
            var code = (DistributedCode)bytes.Skip(4).ToArray()[0];

            Diagnostic.Debug($"Distributed message received: {code} from {connection.Username} ({connection.IPAddress}:{connection.Port})");

            try
            {
                switch (message.Code)
                {
                    case MessageCode.PeerSearchResponse:
                        var searchResponse = SearchResponseSlim.Parse(message);
                        if (SoulseekClient.Searches.TryGetValue(searchResponse.Token, out var search))
                        {
                            search.AddResponse(searchResponse);
                        }

                        break;

                    case MessageCode.PeerBrowseResponse:
                        var browseWaitKey = new WaitKey(MessageCode.PeerBrowseResponse, connection.Username);
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

                    case MessageCode.PeerInfoResponse:
                        var infoResponse = UserInfoResponse.Parse(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.PeerInfoResponse, connection.Username), infoResponse);
                        break;

                    case MessageCode.PeerTransferResponse:
                        var transferResponse = TransferResponse.Parse(message);
                        Console.WriteLine($"Got response from {connection.Username}: {transferResponse.Token}");
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.PeerTransferResponse, connection.Username, transferResponse.Token), transferResponse);
                        break;

                    case MessageCode.PeerQueueDownload:
                        // the end state here is to wait until there's actually a free slot, then send this request to the peer to
                        // let them know we are ready to start the actual transfer.
                        var queueDownloadRequest = QueueDownloadRequest.Parse(message);
                        var (queueAllowed, queueRejectionMessage) = SoulseekClient.Resolvers.QueueDownloadResponse(connection.Username, connection.IPAddress, connection.Port, queueDownloadRequest.Filename);

                        if (!queueAllowed)
                        {
                            await connection.WriteMessageAsync(new QueueFailedResponse(queueDownloadRequest.Filename, queueRejectionMessage)).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.PeerTransferRequest:
                        var transferRequest = TransferRequest.Parse(message);

                        if (transferRequest.Direction == TransferDirection.Upload)
                        {
                            SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.PeerTransferRequest, connection.Username, transferRequest.Filename), transferRequest);
                        }
                        else
                        {
                            var (transferAllowed, transferRejectionMessage) = SoulseekClient.Resolvers.QueueDownloadResponse(connection.Username, connection.IPAddress, connection.Port, transferRequest.Filename);

                            if (!transferAllowed)
                            {
                                await connection.WriteMessageAsync(new TransferResponse(transferRequest.Token, transferRejectionMessage)).ConfigureAwait(false);
                                await connection.WriteMessageAsync(new QueueFailedResponse(transferRequest.Filename, transferRejectionMessage)).ConfigureAwait(false);
                            }
                            else
                            {
                                await connection.WriteMessageAsync(new TransferResponse(transferRequest.Token, "Queued.")).ConfigureAwait(false);
                            }
                        }

                        break;

                    case MessageCode.PeerQueueFailed:
                        var queueFailedResponse = QueueFailedResponse.Parse(message);
                        SoulseekClient.Waiter.Throw(new WaitKey(MessageCode.PeerTransferRequest, connection.Username, queueFailedResponse.Filename), new TransferRejectedException(queueFailedResponse.Message));
                        break;

                    case MessageCode.PeerPlaceInQueueResponse:
                        var placeInQueueResponse = PeerPlaceInQueueResponse.Parse(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.PeerPlaceInQueueResponse, connection.Username, placeInQueueResponse.Filename), placeInQueueResponse);
                        break;

                    case MessageCode.PeerUploadFailed:
                        var uploadFailedResponse = PeerUploadFailedResponse.Parse(message);
                        var msg = $"Download of {uploadFailedResponse.Filename} reported as failed by {connection.Username}.";

                        var download = SoulseekClient.Downloads.Values.FirstOrDefault(d => d.Username == connection.Username && d.Filename == uploadFailedResponse.Filename);
                        if (download != null)
                        {
                            SoulseekClient.Waiter.Throw(new WaitKey(MessageCode.PeerTransferRequest, download.Username, download.Filename), new TransferException(msg));
                            SoulseekClient.Waiter.Throw(download.WaitKey, new TransferException(msg));
                        }

                        Diagnostic.Debug(msg);
                        break;

                    case MessageCode.PeerBrowseRequest:
                        var browseResponse = SoulseekClient.Resolvers.BrowseResponse(connection.Username, connection.IPAddress, connection.Port);
                        await connection.WriteMessageAsync(browseResponse).ConfigureAwait(false);
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled peer message: {message.Code} from {connection.Username} ({connection.IPAddress}:{connection.Port}); {message.Payload.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling peer message: {message.Code} from {connection.Username} ({connection.IPAddress}:{connection.Port}); {ex.Message}", ex);
            }
        }
    }
}
