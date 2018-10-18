namespace Soulseek.NET.Messaging
{
    using System;
    using System.Linq;
    using System.Text;

    public class MessageReader
    {
        private int Position { get; set; } = 8;
        private byte[] Bytes { get; set; }

        public int Length => GetLength();
        public MessageCode Code => GetCode();
        public byte[] Payload => GetPayload();
        public byte[] RawBytes => Bytes;

        public MessageReader(byte[] bytes)
        {
            Bytes = bytes;
        }

        public MessageReader Reset()
        {
            Position = 8;
            return this;
        }

        private byte[] GetPayload()
        {
            return Bytes.Skip(8).ToArray();
        }

        private int GetLength()
        {
            try
            {
                var retVal = BitConverter.ToInt32(Bytes, 0);
                return retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read the message length.", ex);
            }
        }

        private MessageCode GetCode()
        {
            try
            {
                var retVal = BitConverter.ToInt32(Bytes, 4);
                return (MessageCode)retVal;
            }
            catch (Exception ex)
            {
                throw new MessageReadException($"Failed to read the message code of the message.", ex);
            }
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

        public byte[] ReadBytes(int count)
        {
            try
            {
                var retVal = Bytes.Skip(Position).Take(count).ToArray();
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
