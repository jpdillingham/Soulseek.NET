namespace Soulseek.NET.Tests.Unit.Tcp
{
    using System.Net;
    using AutoFixture.Xunit2;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class ConnectionFactoryTests
    {
        [Trait("Category", "GetConnection")]
        [Theory(DisplayName = "GetConnection returns expected IConnection"), AutoData]
        public void GetConnection_Returns_Expected_IConnection(IPAddress ip, int port, ConnectionOptions options)
        {
            var f = new ConnectionFactory();

            var c = f.GetConnection(ip, port, options);

            Assert.Equal(ip, c.IPAddress);
            Assert.Equal(port, c.Port);
            Assert.Equal(options, c.Options);
        }
    }
}
