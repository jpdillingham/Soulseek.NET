namespace Soulseek.NET.Messaging
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    public class Message
    {
        public int Length { get; private set; }
        public MessageCode Code { get; private set; }
        public IReadOnlyCollection<IMessageSegment> Segments => new ReadOnlyCollection<IMessageSegment>(InternalSegments);
        private IList<IMessageSegment> InternalSegments { get; set; } = new List<IMessageSegment>();

        public Message(MessageCode code, params IMessageSegment[] segments)
        {
            Code = code;
            InternalSegments = segments.ToList();
        }
    }
}
