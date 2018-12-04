namespace Soulseek.NET.Tests.Unit
{
    using Newtonsoft.Json;
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

            Assert.Equal(defaultServer, s.Address);
            Assert.Equal(defaultPort, s.Port);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Uses default options with read timeout zero")]
        public void Uses_Default_Options_With_Read_Timeout_Zero()
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
    }
}
