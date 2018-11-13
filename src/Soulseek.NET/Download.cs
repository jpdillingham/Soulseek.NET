namespace Soulseek.NET
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Tcp;

    public sealed class Download
    {
        internal Download(string username, string filename, string ipAddress, int port, DownloadOptions options, IMessageConnection peerConnection = null, ITransferConnection transferConnection = null)
        {
            Username = username;
            Filename = filename;
            IPAddress = ipAddress;
            Port = port;
            Options = options ?? new DownloadOptions();
            PeerConnection = peerConnection ?? new MessageConnection(ConnectionType.Peer, ipAddress, port, Options.ConnectionOptions);
            TransferConnection = transferConnection ?? new TransferConnection(ipAddress, port, Options.ConnectionOptions);
        }

        public string Username { get; private set; }
        public string IPAddress { get; private set; }
        public int Port { get; private set; }
        public string Filename { get; private set; }
        public PeerTransferResponse TransferResponse { get; private set; }
        public PeerTransferRequestResponse TransferRequestResponse { get; private set; }
        public DownloadOptions Options { get; private set; }
        private IMessageConnection PeerConnection { get; set; }
        private ITransferConnection TransferConnection { get; set; }
        public int Token { get; private set; }
        public long FileSize { get; private set; }

        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();

        internal async Task<Download> DownloadAsync(CancellationToken? cancellationToken = null)
        {
            PeerConnection.MessageReceived += OnPeerConnectionMessageReceived;
            PeerConnection.StateChanged += OnPeerConnectionStateChanged;

            TransferConnection.StateChanged += OnTransferConnectionStateChanged;

            try
            {
                var peerTransferResponse = MessageWaiter.Wait<PeerTransferResponse>(MessageCode.PeerTransferResponse, cancellationToken);
                var peerTransferRequestResponse = MessageWaiter.Wait<PeerTransferRequestResponse>(MessageCode.PeerTransferRequest, cancellationToken);

                await PeerConnection.ConnectAsync();

                var token = new Random().Next();
                Console.WriteLine($"Requesting: {token}");
                await PeerConnection.SendAsync(new PeerInitRequest("praetor-2", "P", token).ToMessage(), suppressCodeNormalization: true);
                await PeerConnection.SendAsync(new PeerTransferRequest(TransferDirection.Download, token, Filename).ToMessage());

                TransferResponse = await peerTransferResponse;

                if (TransferResponse.Allowed)
                {
                    Token = TransferResponse.Token;
                    FileSize = TransferResponse.FileSize;
                    Console.WriteLine($"Transfer OK, begin now.");
                    // todo: this
                }
                else
                {
                    Console.WriteLine($"Transfer rejected, wait for request.");
                    TransferRequestResponse = await peerTransferRequestResponse;
                    Token = TransferRequestResponse.Token;
                    FileSize = TransferRequestResponse.Size;

                    await PeerConnection.SendAsync(new PeerTransferResponseRequest(TransferRequestResponse.Token, true, 0, string.Empty).ToMessage());
                }

                return this;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to download {Filename} from {Username}: {ex.Message}");
                return this; // todo throw DownloadException
            }
        }

        public async Task ConnectToPeer(ConnectToPeerResponse response, NetworkEventArgs e)
        {
            var t = new TransferConnection(response.IPAddress.ToString(), response.Port);
            //t.DataReceived += OnTransferConnectionDataReceived;
            //t.StateChanged += OnTransferConnectionStateChanged;

            Console.WriteLine($"[CONNECT TO PEER]: {response.Token}");
            Console.WriteLine($"[OPENING TRANSFER CONNECTION] {t.Address}:{t.Port}");
            await t.ConnectAsync();
            var request = new PierceFirewallRequest(response.Token);
            await t.SendAsync(request.ToMessage().ToByteArray());

            var tokenBytes = await t.ReadAsync(4);
            var token = BitConverter.ToInt32(tokenBytes, 0);

            if (token != Token)
            {
                // todo: think through this.  we don't have the token until we connect, pierce the firewall and read the first 4 bytes.
                // perhaps a DownloadRouter to determine this, or a DownloadQueue to abstract it and handling of Download objects.
                throw new Exception($"Received token {token} doesn't match expected token {TransferResponse.Token}.  The ConnectToPeer response was routed to the wrong place.");
            }

            // write 8 empty bytes.  no idea what this is; captured via WireShark
            // the transfer will not begin until it is sent.
            await t.SendAsync(new byte[8]);

            Console.WriteLine($"Downloading {FileSize} bytes...");
            var destination = System.IO.Path.Combine(@"C:\tmp\", System.IO.Path.GetFileName(Filename));
            var bytes = await t.ReadAsync(FileSize);

            System.IO.File.WriteAllBytes(destination, bytes);
            Console.WriteLine($"File downloaded to {destination}");

            // just wait for now
            await new MessageWaiter().WaitIndefinitely<PeerTransferRequestResponse>(MessageCode.Unknown);
        }

        private void OnPeerConnectionMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Console.WriteLine($"[MESSAGE FROM PEER]: {e.Message.Code}");

            switch (e.Message.Code)
            {
                case MessageCode.PeerTransferResponse:
                    MessageWaiter.Complete(MessageCode.PeerTransferResponse, PeerTransferResponse.Parse(e.Message));
                    break;
                case MessageCode.PeerTransferRequest:
                    try
                    {
                        var x = PeerTransferRequestResponse.Parse(e.Message);
                        MessageWaiter.Complete(MessageCode.PeerTransferRequest, x);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    break;
                default:
                    Console.WriteLine($"[RESPONSE]: {e.Message.Code}");
                    break;
            }
        }

        private void OnPeerConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            if (e.State == ConnectionState.Disconnected && sender is Connection connection)
            {
                Console.WriteLine($"[PEER DISCONNECTED]");
                //MessageWaiter.Throw(MessageCode.Unknown, new Exception("disconnected"));
            }
        }

        private void OnTransferConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"[TRANSFER DATA]");
        }

        private void OnTransferConnectionStateChanged(object sender, ConnectionStateChangedEventArgs e)
        {
            Console.WriteLine($"[TRANSFER CONNECTION]: {e.State}");
        }
    }
}
