namespace Soulseek.NET.Messaging.Responses
{
    using System;

    public class IntegerResponse
    {
        public int Value { get; private set; }

        public static IntegerResponse Parse(Message message)
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
