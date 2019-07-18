namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;

    internal sealed class PeerMessageHandler : IPeerMessageHandler
    {
        public PeerMessageHandler(
            ISoulseekClient soulseekClient,
            IWaiter waiter,
            ConcurrentDictionary<int, Transfer> downloads,
            ConcurrentDictionary<int, Search> searches,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient;
            Waiter = waiter;
            Downloads = downloads;
            Searches = searches;
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new ClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        public event EventHandler<DiagnosticGeneratedEventArgs> DiagnosticGenerated;

        private IDiagnosticFactory Diagnostic { get; }
        private ISoulseekClient SoulseekClient { get; }
        private ConcurrentDictionary<int, Transfer> Downloads { get; }
        private IWaiter Waiter { get; }
        private ConcurrentDictionary<int, Search> Searches { get; }

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
                        if (Searches.TryGetValue(searchResponse.Token, out var search))
                        {
                            search.AddResponse(searchResponse);
                        }

                        break;

                    case MessageCode.Peer.BrowseResponse:
                        var browseWaitKey = new WaitKey(MessageCode.Peer.BrowseResponse, connection.Username);
                        try
                        {
                            Waiter.Complete(browseWaitKey, BrowseResponse.FromByteArray(message));
                        }
                        catch (Exception ex)
                        {
                            Waiter.Throw(browseWaitKey, new MessageReadException("The peer returned an invalid browse response.", ex));
                            throw;
                        }

                        break;

                    case MessageCode.Peer.InfoRequest:
                        var outgoingInfo = await new ClientOptions()
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

                    case MessageCode.Peer.InfoResponse:
                        var incomingInfo = UserInfoResponse.FromByteArray(message);
                        Waiter.Complete(new WaitKey(MessageCode.Peer.InfoResponse, connection.Username), incomingInfo);
                        break;

                    case MessageCode.Peer.TransferResponse:
                        var transferResponse = TransferResponse.FromByteArray(message);
                        Console.WriteLine($"Got response from {connection.Username}: {transferResponse.Token}");
                        Waiter.Complete(new WaitKey(MessageCode.Peer.TransferResponse, connection.Username, transferResponse.Token), transferResponse);
                        break;

                    case MessageCode.Peer.QueueDownload:
                        var queueDownloadRequest = QueueDownloadRequest.FromByteArray(message);

                        var (queueRejected, queueRejectionMessage) =
                            await TryEnqueueDownloadAsync(connection.Username, connection.IPAddress, connection.Port, queueDownloadRequest.Filename).ConfigureAwait(false);

                        if (queueRejected)
                        {
                            await connection.WriteAsync(new QueueFailedResponse(queueDownloadRequest.Filename, queueRejectionMessage).ToByteArray()).ConfigureAwait(false);
                        }

                        break;

                    case MessageCode.Peer.TransferRequest:
                        var transferRequest = TransferRequest.FromByteArray(message);

                        if (transferRequest.Direction == TransferDirection.Upload)
                        {
                            Waiter.Complete(new WaitKey(MessageCode.Peer.TransferRequest, connection.Username, transferRequest.Filename), transferRequest);
                        }
                        else
                        {
                            var (transferRejected, transferRejectionMessage) = await TryEnqueueDownloadAsync(connection.Username, connection.IPAddress, connection.Port, transferRequest.Filename).ConfigureAwait(false);

                            if (transferRejected)
                            {
                                await connection.WriteAsync(new TransferResponse(transferRequest.Token, transferRejectionMessage).ToByteArray()).ConfigureAwait(false);
                                await connection.WriteAsync(new QueueFailedResponse(transferRequest.Filename, transferRejectionMessage).ToByteArray()).ConfigureAwait(false);
                            }
                            else
                            {
                                await connection.WriteAsync(new TransferResponse(transferRequest.Token, "Queued.").ToByteArray()).ConfigureAwait(false);
                            }
                        }

                        break;

                    case MessageCode.Peer.QueueFailed:
                        var queueFailedResponse = QueueFailedResponse.FromByteArray(message);
                        Waiter.Throw(new WaitKey(MessageCode.Peer.TransferRequest, connection.Username, queueFailedResponse.Filename), new TransferRejectedException(queueFailedResponse.Message));
                        break;

                    case MessageCode.Peer.PlaceInQueueResponse:
                        var placeInQueueResponse = PlaceInQueueResponse.FromByteArray(message);
                        Waiter.Complete(new WaitKey(MessageCode.Peer.PlaceInQueueResponse, connection.Username, placeInQueueResponse.Filename), placeInQueueResponse);
                        break;

                    case MessageCode.Peer.UploadFailed:
                        var uploadFailedResponse = UploadFailedResponse.FromByteArray(message);
                        var msg = $"Download of {uploadFailedResponse.Filename} reported as failed by {connection.Username}.";

                        var download = Downloads.Values.FirstOrDefault(d => d.Username == connection.Username && d.Filename == uploadFailedResponse.Filename);
                        if (download != null)
                        {
                            Waiter.Throw(new WaitKey(MessageCode.Peer.TransferRequest, download.Username, download.Filename), new TransferException(msg));
                            Waiter.Throw(download.WaitKey, new TransferException(msg));
                        }

                        Diagnostic.Debug(msg);
                        break;

                    case MessageCode.Peer.BrowseRequest:
                        var browseResponse = await SoulseekClient.Options.BrowseResponseResolver(connection.Username, connection.IPAddress, connection.Port).ConfigureAwait(false);
                        await connection.WriteAsync(browseResponse.ToByteArray()).ConfigureAwait(false);
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
                    .QueueDownloadAction(username, ipAddress, port, filename).ConfigureAwait(false);
            }
            catch (QueueDownloadException ex)
            {
                // pass the exception message through to the remote user only if QueueDownloadException is thrown
                rejected = true;
                rejectionMessage = ex.Message;
            }
            catch (Exception)
            {
                // if any other exception is thrown, return a generic message.  do this to avoid exposing potentially sensitive information that may be contained in the Exception message (filesystem details, etc.)
                rejected = true;
                rejectionMessage = "Enqueue failed due to internal error.";
            }

            return (rejected, rejectionMessage);
        }
    }
}
