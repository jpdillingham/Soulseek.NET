namespace Soulseek.NET.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class MessageBuilder
    {
        private List<byte> Bytes { get; set; } = new List<byte>();

        public MessageBuilder Code(MessageCode code)
        {
            Bytes.AddRange(BitConverter.GetBytes((int)code));
            return this;
        }

        public MessageBuilder WriteByte(byte value)
        {
            Bytes.Add(value);
            return this;
        }

        public MessageBuilder WriteInteger(int value)
        {
            Bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public MessageBuilder WriteLong(long value)
        {
            Bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public MessageBuilder WriteString(string value)
        {
            Bytes.AddRange(BitConverter.GetBytes(value.Length));
            Bytes.AddRange(Encoding.ASCII.GetBytes(value));
            return this;
        }

        public byte[] Build()
        {
            var withLength = new List<byte>(BitConverter.GetBytes(Bytes.Count()));
            withLength.AddRange(Bytes);
            return withLength.ToArray();
        }
    }
}
