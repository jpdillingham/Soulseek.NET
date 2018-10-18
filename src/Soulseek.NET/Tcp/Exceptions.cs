namespace Soulseek.NET.Tcp
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ConnectionException : Exception
    {
        public ConnectionException()
            : base()
        {
        }

        public ConnectionException(string message)
            : base(message)
        {
        }

        public ConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ConnectionStateException : Exception
    {
        public ConnectionStateException()
            : base()
        {
        }

        public ConnectionStateException(string message)
            : base(message)
        {
        }

        public ConnectionStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
