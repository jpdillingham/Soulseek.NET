// <copyright file="ConnectionException.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tcp
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Soulseek.NET.Exceptions;

    /// <summary>
    ///     Represents errors that occur within a TCP connection.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ConnectionException : SoulseekClientException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionException"/> class.
        /// </summary>
        public ConnectionException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionException"/> class with the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The Exception message.</param>
        public ConnectionException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionException"/> class with the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The Exception message.</param>
        /// <param name="innerException">The inner Exception.</param>
        public ConnectionException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur when reading data from a TCP connection.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ConnectionReadException : ConnectionException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionReadException"/> class.
        /// </summary>
        public ConnectionReadException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionReadException"/> class with the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The Exception message.</param>
        public ConnectionReadException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionReadException"/> class with the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The Exception message.</param>
        /// <param name="innerException">The inner Exception.</param>
        public ConnectionReadException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    ///     Represents errors that occur when writing data to a TCP connection.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [Serializable]
    public class ConnectionWriteException : ConnectionException
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionWriteException"/> class.
        /// </summary>
        public ConnectionWriteException()
            : base()
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionWriteException"/> class with the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The Exception message.</param>
        public ConnectionWriteException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionWriteException"/> class with the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The Exception message.</param>
        /// <param name="innerException">The inner Exception.</param>
        public ConnectionWriteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
