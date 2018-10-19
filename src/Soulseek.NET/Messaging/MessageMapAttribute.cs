
namespace Soulseek.NET.Messaging
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MessageMapAttribute : Attribute
    {
        public MessageMapAttribute(MessageCode code)
        {
            Code = code;
        }

        public MessageCode Code { get; private set; }
    }
}
