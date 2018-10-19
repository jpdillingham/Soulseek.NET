namespace Soulseek.NET.Messaging
{
    using System;
    using System.Linq;

    public class Message
    {
        private byte[] Bytes { get; set; }

        public int Length => GetLength();
        public MessageCode Code => GetCode();
        public byte[] Payload => GetPayload();

        public Message(byte[] bytes)
        {
            Bytes = bytes;
        }

        public byte[] ToByteArray()
        {
            return Bytes;
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
    }
}
