﻿// <copyright file="TransferException.cs" company="JP Dillingham">
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
    ///     Represents errors that occur while transferring a file.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class TransferException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferException"/> class.
        /// </summary>
        public TransferException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TransferException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferException"/> class with a specified error message and a
        ///     reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public TransferException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected TransferException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur when a transfer operation would be duplicated.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class DuplicateTransferException : TransferException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DuplicateTransferException"/> class.
        /// </summary>
        public DuplicateTransferException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DuplicateTransferException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public DuplicateTransferException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DuplicateTransferException"/> class with a specified error message
        ///     and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public DuplicateTransferException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DuplicateTransferException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected DuplicateTransferException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur when an operation is attempted against a transfer which is not active.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class TransferNotFoundException : TransferException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferNotFoundException"/> class.
        /// </summary>
        public TransferNotFoundException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferNotFoundException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TransferNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferNotFoundException"/> class with a specified error message and
        ///     a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public TransferNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferNotFoundException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected TransferNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur when a transfer request is rejected.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class TransferRejectedException : TransferException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferRejectedException"/> class.
        /// </summary>
        public TransferRejectedException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferRejectedException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public TransferRejectedException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferRejectedException"/> class with a specified error message and
        ///     a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        ///     The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no
        ///     inner exception is specified.
        /// </param>
        public TransferRejectedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferRejectedException"/> class with serialized data.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected TransferRejectedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}