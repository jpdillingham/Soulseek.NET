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

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ConnectionWriteException : Exception
    {
        public ConnectionWriteException()
            : base()
        {
        }

        public ConnectionWriteException(string message)
            : base(message)
        {
        }

        public ConnectionWriteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ConnectionReadException : Exception
    {
        public ConnectionReadException()
            : base()
        {
        }

        public ConnectionReadException(string message)
            : base(message)
        {
        }

        public ConnectionReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
