// <copyright file="SoulseekClientException.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Exceptions
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.Serialization;

    /// <summary>
    ///     Represents errors that occur while logging in to the Soulseek network.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class AddUserException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AddUserException"/> class.
        /// </summary>
        public AddUserException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AddUserException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AddUserException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AddUserException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public AddUserException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="AddUserException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected AddUserException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur when attempting to fetch the place of a download in a remote queue.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class DownloadPlaceInQueueException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadPlaceInQueueException"/> class.
        /// </summary>
        public DownloadPlaceInQueueException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadPlaceInQueueException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DownloadPlaceInQueueException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadPlaceInQueueException"/> class with a specified error message
        ///     and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public DownloadPlaceInQueueException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadPlaceInQueueException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected DownloadPlaceInQueueException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur due to token collisions.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class DuplicateTokenException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DuplicateTokenException"/> class.
        /// </summary>
        public DuplicateTokenException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DuplicateTokenException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DuplicateTokenException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DuplicateTokenException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public DuplicateTokenException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DuplicateTokenException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected DuplicateTokenException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents an error connecting to a peer due to the peer being offline.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class PeerOfflineException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerOfflineException"/> class.
        /// </summary>
        public PeerOfflineException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerOfflineException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PeerOfflineException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerOfflineException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public PeerOfflineException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PeerOfflineException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected PeerOfflineException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur while attempting to enqueue a download.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class QueueDownloadException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="QueueDownloadException"/> class.
        /// </summary>
        public QueueDownloadException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="QueueDownloadException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public QueueDownloadException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="QueueDownloadException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public QueueDownloadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="QueueDownloadException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected QueueDownloadException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur when fetching the list of chat rooms from the server.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class RoomListException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomListException"/> class.
        /// </summary>
        public RoomListException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomListException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public RoomListException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomListException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public RoomListException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomListException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected RoomListException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur when attempting to inform the server of the number of shared directores and files.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class SharedCountsException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SharedCountsException"/> class.
        /// </summary>
        public SharedCountsException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharedCountsException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SharedCountsException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharedCountsException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public SharedCountsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SharedCountsException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SharedCountsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur during execution of <see cref="SoulseekClient"/> operations.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class SoulseekClientException : Exception
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientException"/> class.
        /// </summary>
        public SoulseekClientException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public SoulseekClientException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public SoulseekClientException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected SoulseekClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur while fetching user connection information.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class UserAddressException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserAddressException"/> class.
        /// </summary>
        public UserAddressException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserAddressException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UserAddressException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserAddressException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public UserAddressException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserAddressException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected UserAddressException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur while fetching user information.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class UserInfoException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInfoException"/> class.
        /// </summary>
        public UserInfoException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInfoException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UserInfoException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInfoException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public UserInfoException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserInfoException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected UserInfoException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur while fetching user status.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class UserStatusException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatusException"/> class.
        /// </summary>
        public UserStatusException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatusException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UserStatusException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatusException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public UserStatusException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="UserStatusException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected UserStatusException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}