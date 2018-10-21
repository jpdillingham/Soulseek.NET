namespace Soulseek.NET.Messaging.Responses
{
    public class IntegerResponse
    {
        public int Value { get; private set; }

        public static IntegerResponse Map(Message message)
        {
            var reader = new MessageReader(message);
            var response = new IntegerResponse()
            {
                Value = reader.ReadInteger()
            };

            return response;
        }
    }
}
