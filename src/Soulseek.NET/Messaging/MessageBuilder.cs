namespace Soulseek.NET.Messaging
{
    using System.Collections.Generic;
    using System.Linq;

    public class MessageBuilder
    {
        private MessageCode Code { get; set; }
        private IList<IMessageSegment> Segments { get; set; } = new List<IMessageSegment>();

        public Message Build()
        {
            return new Message(Code, Segments.ToArray());
        }

        public MessageBuilder MessageCode(MessageCode code)
        {
            Code = code;
            return this;
        }
    }
}
