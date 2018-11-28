namespace Soulseek.NET.Tests.Integration
{
    using Soulseek.NET.Tcp;
    using System.Threading.Tasks;
    using Xunit;

    public class SoulseekClientTests
    {
        [Trait("Category", "Connectivity")]
        [Fact(DisplayName = "Client connects")]
        public async Task Client_Connects()
        {
            var client = new SoulseekClient();

            var ex = await Record.ExceptionAsync(() => client.ConnectAsync());

            Assert.Null(ex);
            Assert.Equal(ConnectionState.Connected, client.State);
        }

        [Trait("Category", "Connectivity")]
        [Fact(DisplayName = "Client disconnects")]
        public async Task Client_Disconnects()
        {
            var client = new SoulseekClient();
            await client.ConnectAsync();

            var ex = Record.Exception(() => client.Disconnect());

            Assert.Null(ex);
            Assert.Equal(ConnectionState.Disconnected, client.State);
        }
    }
}
