namespace Soulseek.NET.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class MessageReader
    {
        private List<byte> Bytes { get; set; }

        public MessageReader(byte[] bytes)
        {
            Bytes = bytes.ToList();
        }
    }
}
