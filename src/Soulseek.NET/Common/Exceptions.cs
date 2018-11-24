// <copyright file="Exceptions.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.NET
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class BrowseException : SoulseekClientException
    {
        public BrowseException()
            : base()
        {
        }

        public BrowseException(string message)
            : base(message)
        {
        }

        public BrowseException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ConnectionException : SoulseekClientException
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
    public class ConnectionReadException : ConnectionException
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
    public class ConnectionStateException : ConnectionException
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
    public class ConnectionWriteException : ConnectionException
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
    public class LoginException : SoulseekClientException
    {
        public LoginException()
            : base()
        {
        }

        public LoginException(string message)
            : base(message)
        {
        }

        public LoginException(string message, Exception innerException)
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

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class MessageException : SoulseekClientException
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
    public class SearchException : SoulseekClientException
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
    public class SoulseekClientException : Exception
    {
        public SoulseekClientException()
            : base()
        {
        }

        public SoulseekClientException(string message)
            : base(message)
        {
        }

        public SoulseekClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }


    [ExcludeFromCodeCoverage]
    [Serializable]
    public class DownloadException : SoulseekClientException
    {
        public DownloadException()
            : base()
        {
        }

        public DownloadException(string message)
            : base(message)
        {
        }

        public DownloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}