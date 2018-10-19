using System;
using System.Linq;
using System.Reflection;

namespace Soulseek.NET.Messaging
{
    public class MessageMapper
    {
        public object Map(Message message)
        {
            var map = GetMapForCode(message.Code);
            return map;
        }

        private Type GetMapForCode(MessageCode code)
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass)
                .Where(t => t.Namespace.Equals(GetType().Namespace + ".Maps"))
                .Where(t => t.IsAssignableFrom(typeof(IMessageMap<>)))
                .Where(t => t.CustomAttributes
                    .Where(c => c.AttributeType == typeof(MessageMapAttribute))
                    .Any(c => c.ConstructorArguments
                        .Where(a => a.ArgumentType == typeof(MessageCode))
                        .Select(v => (MessageCode)v.Value)
                        .SingleOrDefault() == code)
                ).SingleOrDefault();
        }
    }
}
