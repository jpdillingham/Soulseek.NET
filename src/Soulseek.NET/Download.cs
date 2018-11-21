namespace Soulseek.NET
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Requests;
    using Soulseek.NET.Messaging.Responses;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;

    public sealed class Download
    {
        internal Download(string username, string filename, string ipAddress, int port, DownloadOptions options, CancellationToken? cancellationToken, IMessageConnection peerConnection = null, IConnection transferConnection = null)
        {
            Username = username;
            Filename = filename;
            IPAddress = ipAddress;
            Port = port;
            Options = options ?? new DownloadOptions();
            CancellationToken = cancellationToken;

            PeerConnection = peerConnection ?? new MessageConnection(ConnectionType.Peer, ipAddress, port, Options.ConnectionOptions);
            TransferConnection = transferConnection ?? new Connection(ipAddress, port, Options.ConnectionOptions);

            peerConnection.ConnectHandler = HandleConnect;
            PeerConnection.DisconnectHandler = HandleDisconnect;
            PeerConnection.MessageHandler = HandleMessage;
        }

        public string Username { get; private set; }
        public string IPAddress { get; private set; }
        public int Port { get; private set; }
        public string Filename { get; private set; }
        public PeerTransferResponseIncoming TransferResponseIncoming { get; private set; }
        public PeerTransferRequestIncoming TransferRequestIncoming { get; private set; }
        public DownloadOptions Options { get; private set; }
        private IMessageConnection PeerConnection { get; set; }
        private IConnection TransferConnection { get; set; }
        public int Token { get; private set; }
        public long FileSize { get; private set; }
        public CancellationToken? CancellationToken { get; private set; }

        private MessageWaiter MessageWaiter { get; set; } = new MessageWaiter();

        private async void HandleConnect(IMessageConnection connection)
        {
            Console.WriteLine($"[DOWNLOAD CONNECT]: {Username}");

            var token = new Random().Next();
            Console.WriteLine($"[{Filename}] Requesting: {token}");
            await connection.SendMessageAsync(new PeerInitRequest("praetor-2", "P", token).ToMessage(), suppressCodeNormalization: true);
            await connection.SendMessageAsync(new PeerTransferRequestOutgoing(TransferDirection.Download, token, Filename).ToMessage());

            try
            {
                var peerTransferResponse = MessageWaiter.WaitIndefinitely<PeerTransferResponseIncoming>(MessageCode.PeerTransferResponse, CancellationToken);
                var peerTransferRequestResponse = MessageWaiter.WaitIndefinitely<PeerTransferRequestIncoming>(MessageCode.PeerTransferRequest, CancellationToken);

                TransferResponseIncoming = await peerTransferResponse;

                if (TransferResponseIncoming.Allowed)
                {
                    Token = TransferResponseIncoming.Token;
                    FileSize = TransferResponseIncoming.FileSize;
                    Console.WriteLine($"Transfer OK, begin now.");
                }
                else
                {
                    Console.WriteLine($"[{Filename}] Transfer rejected, wait for request.");
                    TransferRequestIncoming = await peerTransferRequestResponse; // when ready, peer will initiate transfer

                    Token = TransferRequestIncoming.Token;
                    FileSize = TransferRequestIncoming.Size;

                    await connection.SendMessageAsync(new PeerTransferResponseOutgoing(TransferRequestIncoming.Token, true, 0, string.Empty).ToMessage());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Filename}] Failed to download {Filename} from {Username}: {ex.Message}");
            }
        }

        private async void HandleDisconnect(IMessageConnection connection, string message)
        {
            Console.WriteLine($"[{Filename}] [PEER DISCONNECTED]");
            //MessageWaiter.Throw(MessageCode.Unknown, new Exception("disconnected"));
        }

        internal async Task StartDownload(IConnection t)
        {
            // write 8 empty bytes.  no idea what this is; captured via WireShark
            // the transfer will not begin until it is sent.
            await t.SendAsync(new byte[8]);

            Console.WriteLine($"[{Filename}] Downloading {FileSize} bytes...");
            var destination = System.IO.Path.Combine(@"C:\tmp\", System.IO.Path.GetFileName(Filename));
            var bytes = await t.ReadAsync(FileSize);

            System.IO.File.WriteAllBytes(destination, bytes);
            Console.WriteLine($"[{Filename}] File downloaded to {destination}");

            // just wait for now
            //await new MessageWaiter().WaitIndefinitely<PeerTransferRequestResponse>(MessageCode.Unknown);
            t.Disconnect($"[{Filename}] Download complete.");
        }

        public async Task ConnectToPeer(ConnectToPeerResponse response, NetworkEventArgs e)
        {
            var t = new Connection(response.IPAddress.ToString(), response.Port);

            Console.WriteLine($"[{Filename}] [CONNECT TO PEER]: {response.Token}");
            Console.WriteLine($"[{Filename}] [OPENING TRANSFER CONNECTION] {t.Address}:{t.Port}");
            await t.ConnectAsync();
            var request = new PierceFirewallRequest(response.Token);
            await t.SendAsync(request.ToMessage().ToByteArray());

            var tokenBytes = await t.ReadAsync(4);
            var token = BitConverter.ToInt32(tokenBytes, 0);

            if (token != Token)
            {
                // todo: think through this.  we don't have the token until we connect, pierce the firewall and read the first 4 bytes.
                // perhaps a DownloadRouter to determine this, or a DownloadQueue to abstract it and handling of Download objects.
                throw new Exception($"Received token {token} doesn't match expected token {TransferResponseIncoming.Token}.  The ConnectToPeer response was routed to the wrong place.");
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
            //await new MessageWaiter().WaitIndefinitely<PeerTransferRequestResponse>(MessageCode.Unknown);
            t.Disconnect("Download complete.");
        }

        private void HandleMessage(IMessageConnection connection, Message message)
        {
            Console.WriteLine($"[{Filename}] [MESSAGE FROM PEER]: {message.Code}");

            switch (message.Code)
            {
                case MessageCode.PeerTransferResponse:
                    MessageWaiter.Complete(MessageCode.PeerTransferResponse, PeerTransferResponseIncoming.Parse(message));
                    break;
                case MessageCode.PeerTransferRequest:
                    try
                    {
                        var x = PeerTransferRequestIncoming.Parse(message);
                        Console.WriteLine($"[{Filename}] Completing PTRR");
                        MessageWaiter.Complete(MessageCode.PeerTransferRequest, x);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    break;
                default:
                    Console.WriteLine($"[{Filename}] [RESPONSE]: {message.Code}");
                    break;
            }
        }

        private void OnTransferConnectionDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine($"[{Filename}] [TRANSFER DATA]");
        }
    }
}
