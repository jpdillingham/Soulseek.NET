namespace Soulseek.NET.Messaging
{
    using System;
    using System.Linq;
    using System.Text;

    public class MessageReader
    {
        private int Position { get; set; } = 0;
        private byte[] Bytes { get; set; }

        public MessageReader(byte[] bytes)
        {
            Bytes = bytes;
        }

        public MessageReader Reset()
        {
            Position = 0;
            return this;
        }

        public int ReadByte()
        {
            try
            {
                var retVal = Bytes[Position];
                Position += 1;
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
                var retVal = BitConverter.ToInt32(Bytes, Position);
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
                var retVal = BitConverter.ToInt64(Bytes, Position);
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
                var bytes = Bytes.Skip(Position).Take(length).ToArray();
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
    }
}
