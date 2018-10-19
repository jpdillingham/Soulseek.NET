
namespace Soulseek.NET.Messaging
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MessageResponseAttribute : Attribute
    {
        public MessageResponseAttribute(MessageCode code)
        {
            Code = code;
        }

        public MessageCode Code { get; private set; }
    }
}
