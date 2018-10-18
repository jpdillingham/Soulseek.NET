namespace Soulseek.NET
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
}
