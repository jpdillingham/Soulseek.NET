namespace Soulseek.NET
{
    using System;
    using System.Diagnostics.CodeAnalysis;


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
}
