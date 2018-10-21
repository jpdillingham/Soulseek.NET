namespace Soulseek.NET.Messaging
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MessageHandlerAttribute : Attribute
    {
        public MessageHandlerAttribute(MessageCode code)
        {
            Code = code;
        }

        public MessageCode Code { get; private set; }
    }
}
