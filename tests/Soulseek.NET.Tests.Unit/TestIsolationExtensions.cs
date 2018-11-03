namespace Soulseek.NET.Tests.Unit
{
    using System;
    using System.Reflection;

    public static class TestIsolationExtensions
    {
        public static T GetField<T>(this object target, string fieldName, BindingFlags flags)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, flags);

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

        public static T GetNonPublicField<T>(this object target, string fieldName)
        {
            return GetField<T>(target, fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static T GetNonPublicProperty<T>(this object target, string propertyName)
        {
            return GetProperty<T>(target, propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static T GetNonPublicStaticField<T>(this object target, string fieldName)
        {
            return GetField<T>(target, fieldName, BindingFlags.NonPublic | BindingFlags.Static);
        }

        public static T GetNonPublicStaticProperty<T>(this object target, string propertyName)
        {
            return GetProperty<T>(target, propertyName, BindingFlags.NonPublic | BindingFlags.Static);
        }

        public static T GetProperty<T>(this object target, string propertyName, BindingFlags flags)
        {
            var type = target.GetType();
            var field = type.GetProperty(propertyName, flags);

            if (field == default(FieldInfo))
            {
                throw new ArgumentException($"No such property '{propertyName}' exists on target Type {type.Name}.", nameof(propertyName));
            }

            try
            {
                return (T)field.GetValue(target);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to get private property '{propertyName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static void InvokePrivateMethod(this object target, string methodName, params object[] args)
        {
            var type = target.GetType();

            try
            {
                GetPrivateMethod(type, methodName).Invoke(target, args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to invoke private method '{methodName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static T InvokePrivateMethod<T>(this object target, string methodName, params object[] args)
        {
            var type = target.GetType();

            try
            {
                return (T)GetPrivateMethod(type, methodName).Invoke(target, args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to invoke private method '{methodName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static void SetNonPublicField(this object target, string fieldName, object value)
        {
            var type = target.GetType();
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);

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
                throw new Exception($"Failed to inject private field '{fieldName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        public static void SetNonPublicProperty(this object target, string propertyName, object value)
        {
            var type = target.GetType();
            var property = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == default(FieldInfo))
            {
                throw new ArgumentException($"No such property '{propertyName}' exists on target Type {type.Name}.", nameof(propertyName));
            }

            try
            {
                property.SetValue(target, value);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to inject private property '{propertyName}' on target Type {type.Name}.  See inner Exception for details.", ex);
            }
        }

        private static MethodInfo GetPrivateMethod(Type type, string methodName)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == default(MethodInfo))
            {
                throw new ArgumentException($"No such method '{methodName}' exists on target Type {type.Name}.", nameof(methodName));
            }

            return method;
        }
    }
}