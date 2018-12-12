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
}
