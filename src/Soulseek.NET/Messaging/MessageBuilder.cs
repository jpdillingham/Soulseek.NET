namespace Soulseek.NET.Messaging
{
    using System.Collections.Generic;
    using System.Linq;

    public class MessageBuilder
    {
        private bool initialized = false;
        private MessageCode code = MessageCode.Unknown;
        private IList<IMessageSegment> segments = new List<IMessageSegment>();

        public Message Build()
        {
            return new Message(code, segments.ToArray());
        }

        public MessageBuilder Code(MessageCode code)
        {
            initialized = true;
            this.code = code;
            return this;
        }

        public MessageBuilder Segment(IMessageSegment segment)
        {
            initialized = true;
            segments.Add(segment);
            return this;
        }

        public MessageBuilder Message(Message message)
        {
            if (initialized)
            {
                // throw
            }

            code = message.Code;
            segments = message.Segments.ToList();
            return this;
        }

        public MessageBuilder Bytes(byte[] bytes)
        {
            if (initialized)
            {
                // throw
            }

            // todo: parse byte array, set code and segments
            return this;
        }
    }
}
