namespace Soulseek.NET.Messaging.Responses
{
    public sealed class Integer
    {
        public static int Parse(Message message)
        {
            var reader = new MessageReader(message);
            return reader.ReadInteger();
        }
    }
}
