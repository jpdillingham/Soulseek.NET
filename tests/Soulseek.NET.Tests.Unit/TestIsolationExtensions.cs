namespace Soulseek.NET.Tests.Unit
{
    using System;
    using System.Reflection;

    public static class TestIsolationExtensions
    {
        private static readonly BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static T GetField<T>(this object target, string fieldName)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, bindingFlags);

            if (field == default(FieldInfo))
            {
                throw new ArgumentException($"No such field '{fieldName}' exists on target Type {type.Name}.", nameof(fieldName));
            }

            try
            {
                return (T)field.GetValue(target);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get field '{fieldName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static T GetProperty<T>(this object target, string propertyName)
        {
            var type = target.GetType();
            var property = type.GetProperty(propertyName, bindingFlags);

            if (property == default(PropertyInfo))
            {
                throw new ArgumentException($"No such property '{propertyName}' exists on target Type {type.Name}.", nameof(propertyName));
            }

            try
            {
                return (T)property.GetValue(target);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get property '{propertyName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static void InvokeMethod(this object target, string methodName, params object[] args)
        {
            var type = target.GetType();

            try
            {
                GetMethod(type, methodName, bindingFlags).Invoke(target, args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to invoke method '{methodName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static T InvokeMethod<T>(this object target, string methodName, params object[] args)
        {
            var type = target.GetType();

            try
            {
                return (T)GetMethod(type, methodName, bindingFlags).Invoke(target, args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to invoke method '{methodName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static void SetField(this object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, bindingFlags);

            if (field == default(FieldInfo))
            {
                throw new ArgumentException($"No such field '{fieldName}' exists on target Type {type.Name}.", nameof(fieldName));
            }

            try
            {
                field.SetValue(target, value);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set field '{fieldName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static void SetProperty(this object target, string propertyName, object value)
        {
            var type = target.GetType();
            var property = type.GetProperty(propertyName, bindingFlags);

            if (property == default(PropertyInfo))
            {
                throw new ArgumentException($"No such property '{propertyName}' exists on target Type {type.Name}.", nameof(propertyName));
            }

            try
            {
                property.SetValue(target, value);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to set property '{propertyName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static void RaiseEvent(this object target, string eventName, object eventArgs)
        {
            var type = target.GetType();
            var @event = (MulticastDelegate)type.GetField(eventName, bindingFlags)?.GetValue(target);

            if (@event == null)
            {
                throw new ArgumentException($"No such event '{eventName}' exists on target Type {type.Name}.", nameof(eventName));
            }

            try
            {
                foreach (var handler in @event?.GetInvocationList())
                {
                    handler.Method.Invoke(handler.Target, new object[] { target, eventArgs });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to raise event '{eventName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        private static MethodInfo GetMethod(Type type, string methodName, BindingFlags flags)
        {
            var method = type.GetMethod(methodName, flags);

            if (method == default(MethodInfo))
            {
                throw new ArgumentException($"No such method '{methodName}' exists on target Type {type.Name}.", nameof(methodName));
            }

            return method;
        }
    }
}