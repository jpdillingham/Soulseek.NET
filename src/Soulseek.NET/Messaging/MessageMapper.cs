using System;
using System.Linq;
using System.Reflection;

namespace Soulseek.NET.Messaging
{
    public class MessageMapper
    {
        public object MapResponse(Message message)
        {
            var type = GetResponseType(message.Code);
            var method = type.GetMethod("Map");
            var response = Activator.CreateInstance(type);
            method.Invoke(response, new[] { message });

            return response;
        }

        private Type GetResponseType(MessageCode code)
        {
            return Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsClass)
                .Where(t => t.Namespace.Equals(GetType().Namespace + ".Responses"))
                .Where(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType)
                    .Any(i => i.GetGenericTypeDefinition() == typeof(IMessageResponse<>)))
                .Where(t => t.CustomAttributes
                    .Where(c => c.AttributeType == typeof(MessageResponseAttribute))
                    .Any(c => c.ConstructorArguments
                        .Where(a => a.ArgumentType == typeof(MessageCode))
                        .Select(v => (MessageCode)v.Value)
                        .SingleOrDefault() == code))
                .SingleOrDefault();
        }
    }
}
