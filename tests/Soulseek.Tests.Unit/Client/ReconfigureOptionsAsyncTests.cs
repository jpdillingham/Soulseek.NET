namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Moq;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class ReconfigureOptionsAsyncTests
    {
        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Throws ArgumentNullException given null patch")]
        public async Task Throws_ArgumentNullException_Given_Null_Patch()
        {
            var (client, _) = GetFixture();

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(null));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentNullException>(ex);
                Assert.Equal("patch", ((ArgumentNullException)ex).ParamName);
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Throws ArgumentException given listen port which can not be bound")]
        public async Task Throws_ArgumentNullException_Given_Listen_Port_Which_Can_Not_Be_Bound()
        {
            var (client, mocks) = GetFixture();

            var port = Mocks.Port;
            var patch = new SoulseekClientOptionsPatch(listenPort: port);

            Listener listener = null;

            try
            {
                // listen on the port to bind it
                listener = new Listener(port, new ConnectionOptions());
                listener.Start();

                using (client)
                {
                    var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(patch));

                    Assert.NotNull(ex);
                    Assert.IsType<ArgumentException>(ex);
                    Assert.True(ex.Message.ContainsInsensitive($"failed to start listening on port {port}"));
                }
            }
            finally
            {
                listener?.Stop();
            }
        }

        [Trait("Category", "ReconfigureOptions")]
        [Fact(DisplayName = "Does not throw given empty patch")]
        public async Task Does_Not_Throw_Given_Empty_Patch()
        {
            var (client, _) = GetFixture();

            var patch = new SoulseekClientOptionsPatch();

            using (client)
            {
                var ex = await Record.ExceptionAsync(() => client.ReconfigureOptionsAsync(patch));

                Assert.Null(ex);
            }
        }

        private (SoulseekClient client, Mocks Mocks) GetFixture(SoulseekClientOptions clientOptions = null)
        {
            var mocks = new Mocks();
            var client = new SoulseekClient(
                options: clientOptions ?? new SoulseekClientOptions(enableListener: false));

            return (client, mocks);
        }

        private class Mocks
        {
            public Mocks()
            {
            }

            private static readonly Random Rng = new Random();
            public static int Port => Rng.Next(1024, IPEndPoint.MaxPort);

            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
        }
    }
}
