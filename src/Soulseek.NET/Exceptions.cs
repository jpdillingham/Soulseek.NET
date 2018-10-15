namespace Soulseek.NET
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class MessageFormatException : Exception
    {
        public MessageFormatException()
            : base()
        {
        }

        public MessageFormatException(string message)
            : base(message)
        {
        }

        public MessageFormatException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
