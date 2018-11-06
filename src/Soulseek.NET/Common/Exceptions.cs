namespace Soulseek.NET.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ServerException : Exception
    {
        public ServerException()
            : base()
        {
        }

        public ServerException(string message)
            : base(message)
        {
        }

        public ServerException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class SearchException : Exception
    {
        public SearchException()
            : base()
        {
        }

        public SearchException(string message)
            : base(message)
        {
        }

        public SearchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

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

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class MessageException : Exception
    {
        public MessageException()
            : base()
        {
        }

        public MessageException(string message)
            : base(message)
        {
        }

        public MessageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class MessageReadException : MessageException
    {
        public MessageReadException()
            : base()
        {
        }

        public MessageReadException(string message)
            : base(message)
        {
        }

        public MessageReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class MessageBuildException : MessageException
    {
        public MessageBuildException()
            : base()
        {
        }

        public MessageBuildException(string message)
            : base(message)
        {
        }

        public MessageBuildException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class MessageTimeoutException : MessageException
    {
        public MessageTimeoutException()
            : base()
        {
        }

        public MessageTimeoutException(string message)
            : base(message)
        {
        }

        public MessageTimeoutException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class MessageCancelledException : MessageException
    {
        public MessageCancelledException()
            : base()
        {
        }

        public MessageCancelledException(string message)
            : base(message)
        {
        }

        public MessageCancelledException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
