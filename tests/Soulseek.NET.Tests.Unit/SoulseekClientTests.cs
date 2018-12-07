namespace Soulseek.NET.Tests.Unit
{
    using Moq;
    using Newtonsoft.Json;
    using Soulseek.NET.Messaging.Tcp;
    using Soulseek.NET.Tcp;
    using System;
    using Xunit;

    public class SoulseekClientTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with defaults for minimal constructor")]
        public void Instantiates_With_Defaults_For_Minimal_Constructor()
        {
            var s = new SoulseekClient();

            var defaultServer = s.GetField<string>("DefaultAddress");
            var defaultPort = s.GetField<int>("DefaultPort");

            Assert.Equal(defaultServer, s.Address);
            Assert.Equal(defaultPort, s.Port);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates with read timeout zero")]
        public void Instantiates_With_Read_Timeout_Zero()
        {
            var s = new SoulseekClient();

            var defaultOptions = new SoulseekClientOptions();
            defaultOptions.ConnectionOptions.ReadTimeout = 0;

            Assert.Equal(JsonConvert.SerializeObject(defaultOptions), JsonConvert.SerializeObject(s.Options));
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates without exception")]
        public void Instantiates_Without_Exception()
        {
            SoulseekClient s = null;

            var ex = Record.Exception(() => s = new SoulseekClient());

            Assert.Null(ex);
            Assert.NotNull(s);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Ensures ReadTimeout is zero")]
        public void Ensures_ReadTimeout_Is_Zero()
        {
            var options = new SoulseekClientOptions();
            options.ConnectionOptions.ReadTimeout = 5;

            var s = new SoulseekClient(options: options);

            Assert.Equal(0, s.Options.ConnectionOptions.ReadTimeout);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "State is Disconnected initially")]
        public void State_Is_Disconnected_Initially()
        {
            var s = new SoulseekClient();

            Assert.Equal(SoulseekClientState.Disconnected, s.State);
        }

        [Trait("Category", "Connect")]
        [Theory(DisplayName = "Connect fails if connected or transitioning")]
        [InlineData(ConnectionState.Connected)]
        [InlineData(ConnectionState.Connecting)]
        [InlineData(ConnectionState.Disconnecting)]
        public async void Connect_Fails_If_Connected_Or_Transitioning(ConnectionState connectionState)
        {
            var c = new Mock<IMessageConnection>();
            c.Setup(m => m.State).Returns(connectionState);

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object);

            var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

            Assert.NotNull(ex);
            Assert.IsType<ConnectionStateException>(ex);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect throws when TcpConnection throws")]
        public async void Connect_Throws_When_TcpConnection_Throws()
        {
            var c = new Mock<IMessageConnection>();
            c.Setup(m => m.ConnectAsync()).Throws(new ConnectionException());

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object);

            var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

            Assert.NotNull(ex);
            Assert.IsType<ConnectionException>(ex);
        }

        [Trait("Category", "Connect")]
        [Fact(DisplayName = "Connect succeeds when TcpConnection succeeds")]
        public async void Connect_Succeeds_When_TcpConnection_Succeeds()
        {
            var c = new Mock<IMessageConnection>();

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object);

            var ex = await Record.ExceptionAsync(async () => await s.ConnectAsync());

            Assert.Null(ex);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiation throws on a bad address")]
        public void Instantiation_Throws_On_A_Bad_Address()
        {
            var ex = Record.Exception(() => new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), new SoulseekClientOptions()));

            Assert.NotNull(ex);
            Assert.IsType<SoulseekClientException>(ex);
        }

        [Trait("Category", "Disconnect")]
        [Fact(DisplayName = "Disconnect disconnects")]
        public async void Disconnect_Disconnects()
        {
            var c = new Mock<IMessageConnection>();

            var s = new SoulseekClient(Guid.NewGuid().ToString(), new Random().Next(), serverConnection: c.Object);
            await s.ConnectAsync();

            var ex = Record.Exception(() => s.Disconnect());

            Assert.Null(ex);
            Assert.Equal(SoulseekClientState.Disconnected, s.State);
        }
    }
}
