namespace Soulseek.NET.Messaging.Responses
{
    using System;

    public class Integer
    {
        public int Value { get; private set; }

        public static Integer Parse(Message message)
        {
            var reader = new MessageReader(message);
            var response = new Integer()
            {
                Value = reader.ReadInteger()
            };

            return response;
        }
    }
}
