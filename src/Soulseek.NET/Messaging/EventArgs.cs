namespace Soulseek.NET.Messaging
{
    public class RawMessageReceivedEventArgs
    {
        public MessageReader Reader { get; set; }
    }

    public class MessageReceivedEventArgs
    {
        public MessageCode Code { get; set; }
        public object Response { get; set; }
    }
}
