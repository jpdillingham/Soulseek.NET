namespace Soulseek.NET.Messaging
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    public class MessageHandler
    {
        public static bool TryGetHandler(object parent, MessageCode code, out MessageHandler handler)
        {
            try
            {
                handler = new MessageHandler
                {
                    Method = parent.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(m => m.CustomAttributes
                        .Where(c => c.AttributeType == typeof(MessageHandlerAttribute))
                        .Any(c => c.ConstructorArguments
                            .Where(a => a.ArgumentType == typeof(MessageCode))
                            .Select(v => (MessageCode)v.Value)
                            .SingleOrDefault() == code))
                    .SingleOrDefault()
                };

                return handler.Method != null;
            }
            catch (Exception)
            {
                handler = null;
                return false;
            }
        }

        public MethodInfo Method { get; private set; }

        public async Task Invoke(object instance, Message message, object response)
        {
            try
            {
                await Task.Run(() => Method.Invoke(instance, new object[] { message, response }));
            }
            catch (Exception ex)
            {
                throw new MessageHandlerException($"Failed to invoke Message handler for {message.Code}: {ex.Message}", ex);
            }
        }
    }
}
