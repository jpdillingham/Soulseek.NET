namespace Soulseek.NET.Tests.Unit.Messaging
{
    using Soulseek.NET.Messaging;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xunit;

    public class MessageBuilderTests
    {
        [Fact]
        public void Compression_Works()
        {
            var txt = Guid.NewGuid().ToString();
            var num = new Random().Next();
            var txt2 = Guid.NewGuid().ToString();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerInfoRequest)
                .WriteString(txt)
                .WriteInteger(num)
                .WriteString(txt2)
                .Compress()
                .Build();

            var reader = new MessageReader(msg);

            reader.Decompress();

            Assert.Equal(txt, reader.ReadString());
            Assert.Equal(num, reader.ReadInteger());
            Assert.Equal(txt2, reader.ReadString());
        }
    }
}
