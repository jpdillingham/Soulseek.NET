namespace Soulseek.NET.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    
    public class MessageFactory
    {
        public Message FromBytes(byte[] bytes)
        {
            var length = GetMessageLength(bytes);

            var builder = new MessageBuilder()
                .Code(GetMessageCode(bytes));
        }

        private int GetMessageLength(byte[] bytes)
        {
            try
            {
                return Convert.ToInt32(bytes.Take(4));
            }
            catch (Exception ex)
            {
                throw new MessageFormatException($"The Message length is not a valid integer.", ex);
            }
        }

        private MessageCode GetMessageCode(byte[] bytes)
        {
            IEnumerable<byte> chunk = default(IEnumerable<byte>);
            var codeNum = 0;

            try
            {
                chunk = bytes.Skip(4).Take(4);
            }
            catch (Exception ex)
            {
                throw new MessageFormatException($"The Message does not contain a Message Code.");
            }

            try
            {
                codeNum = Convert.ToInt32(chunk);
            }
            catch (Exception ex)
            {
                throw new MessageFormatException($"The Message Code (0x{chunk.ToHexString()}) is not a valid integer.", ex);
            }

            try
            {
                return (MessageCode)codeNum;
            }
            catch (Exception)
            {
                return MessageCode.Unknown;
            }
        }
    }
}
