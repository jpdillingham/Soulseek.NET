namespace Soulseek.NET.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class MessageBuilder
    {
        private bool Initialized { get; set; } = false;
        private List<byte> Bytes { get; set; } = new List<byte>();

        public MessageBuilder()
        {
        }

        public MessageBuilder Code(MessageCode code)
        {
            if (Initialized)
            {
                throw new MessageBuildException($"The Message Code may only be set once.");
            }

            Initialized = true;

            Bytes.AddRange(BitConverter.GetBytes((int)code));
            return this;
        }

        public MessageBuilder Code(byte code)
        {
            if (Initialized)
            {
                throw new MessageBuildException($"The Message Code may only be set once.");
            }

            Initialized = true;

            Bytes.Add(code);
            return this;
        }

        public MessageBuilder WriteByte(byte value)
        {
            EnsureInitialized();

            Bytes.Add(value);
            return this;
        }

        public MessageBuilder WriteBytes(byte[] values)
        {
            EnsureInitialized();

            Bytes.AddRange(values);
            return this;
        }

        public MessageBuilder WriteInteger(int value)
        {
            EnsureInitialized();

            Bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public MessageBuilder WriteLong(long value)
        {
            EnsureInitialized();

            Bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public MessageBuilder WriteString(string value)
        {
            EnsureInitialized();

            Bytes.AddRange(BitConverter.GetBytes(value.Length));
            Bytes.AddRange(Encoding.ASCII.GetBytes(value));
            return this;
        }

        public Message Build()
        {
            var withLength = new List<byte>(BitConverter.GetBytes(Bytes.Count()));
            withLength.AddRange(Bytes);
            return new Message(withLength.ToArray());
        }

        private void EnsureInitialized()
        {
            if (!Initialized)
            {
                throw new MessageBuildException($"The Message must be initialized with a Code prior to writing data.");
            }
        }
    }
}
