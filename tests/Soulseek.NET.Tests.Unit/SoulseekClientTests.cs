namespace Soulseek.NET.Tests.Unit
{
    using Xunit;

    public class SoulseekClientTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Uses defaults for minimal constructor")]
        public void Uses_Defaults_For_Minimal_Constructor()
        {
            var s = new SoulseekClient();

            var defaultServer = s.GetField<string>("DefaultAddress");
            var defaultPort = s.GetField<int>("DefaultPort");
            var defaultOptions = new SoulseekClientOptions();

            Assert.Equal(defaultServer, s.Address);
            Assert.Equal(defaultPort, s.Port);
            Assert.Equal(defaultOptions, s.Options);
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
    }
}
