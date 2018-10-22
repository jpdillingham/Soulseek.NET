namespace Soulseek.NET.Messaging
{
    using Soulseek.NET.Zlib;
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class MessageReader
    {
        public MessageReader(Message message)
        {
            Message = message;
        }

        public MessageReader(byte[] bytes)
            : this(new Message(bytes))
        {
        }

        public MessageCode Code => Message.Code;
        public int Length => Message.Length;
        public byte[] Payload => Message.Payload;
        private Message Message { get; set; }
        private int Position { get; set; } = 0;

        public MessageReader Decompress()
        {
            byte[] decompressedPayload;

            Decompress(Payload, out decompressedPayload);

            Message = new MessageBuilder()
                .Code(Code)
                .WriteBytes(decompressedPayload)
                .Build();

            return this;
        }

        public int ReadByte()
        {
            try
            {
                var retVal = Payload[Position];
                Position += 1;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a byte from position {Position} of the message.", ex);
            }
        }

        public byte[] ReadBytes(int count)
        {
            try
            {
                var retVal = Payload.Skip(Position).Take(count).ToArray();
                Position += count;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a byte from position {Position} of the message.", ex);
            }
        }

        public int ReadInteger()
        {
            try
            {
                var retVal = BitConverter.ToInt32(Payload, Position);
                Position += 4;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read an integer (4 bytes) from position {Position} of the message.", ex);
            }
        }

        public long ReadLong()
        {
            try
            {
                var retVal = BitConverter.ToInt64(Payload, Position);
                Position += 8;
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a long integer (8 bytes) from position {Position} of the message.", ex);
            }
        }

        public string ReadString()
        {
            var length = 0;

            try
            {
                length = ReadInteger();
                var bytes = Payload.Skip(Position).Take(length).ToArray();
                var retVal = Encoding.ASCII.GetString(bytes);
                Position += length;
                return retVal;
            }
            catch (MessageReadException ex)
            {
                throw new MessageReadException($"Failed to read the length of the requested string from position {Position} of the message.", ex);
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read a string of length {length} from position {Position} of the message.", ex);
            }
        }

        public MessageReader Reset()
        {
            Position = 0;
            return this;
        }

        private void Decompress(byte[] inData, out byte[] outData)
        {
            void copyStream(Stream input, Stream output)
            {
                byte[] buffer = new byte[2000];
                int len;
                while ((len = input.Read(buffer, 0, 2000)) > 0)
                {
                    output.Write(buffer, 0, len);
                }
                output.Flush();
            }

            using (var outMemoryStream = new MemoryStream())
            using (var outZStream = new ZOutputStream(outMemoryStream))
            using (var inMemoryStream = new MemoryStream(inData))
            {
                copyStream(inMemoryStream, outZStream);
                outZStream.finish();
                outData = outMemoryStream.ToArray();
            }
        }
    }
}