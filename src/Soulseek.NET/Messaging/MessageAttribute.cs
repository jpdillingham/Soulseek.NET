
namespace Soulseek.NET.Messaging
{
    using System;

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class MessageAttribute : Attribute
    {
        public MessageAttribute(MessageCode code)
        {
            Code = code;
        }

        public MessageCode Code { get; private set; }
    }
}
