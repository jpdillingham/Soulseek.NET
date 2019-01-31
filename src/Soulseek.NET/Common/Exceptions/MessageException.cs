// <copyright file="MessageException.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Exceptions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

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

        protected MessageException(SerializationInfo info, StreamingContext context)
            : base(info, context)
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

        protected MessageBuildException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class MessageCompressionException : MessageException
    {
        public MessageCompressionException()
            : base()
        {
        }

        public MessageCompressionException(string message)
            : base(message)
        {
        }

        public MessageCompressionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MessageCompressionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
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

        protected MessageReadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
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

        protected MessageTimeoutException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
