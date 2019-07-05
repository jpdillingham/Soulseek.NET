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

        public async void HandleMessage(object sender, byte[] message)
        {
            var connection = (IMessageConnection)sender;
            var code = new MessageReader<DistributedCode>(message).ReadCode();

            Diagnostic.Debug($"Distributed message received: {code} from {connection.Username} ({connection.IPAddress}:{connection.Port})");

            try
            {
                switch (code)
                {
                    default:
                        Diagnostic.Debug($"Unhandled distributed message: {code} from {connection.Username} ({connection.IPAddress}:{connection.Port}); {message.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling distributed message: {code} from {connection.Username} ({connection.IPAddress}:{connection.Port}); {ex.Message}", ex);
            }
        }
    }
}
