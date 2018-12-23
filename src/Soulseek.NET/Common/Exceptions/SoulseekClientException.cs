// <copyright file="SoulseekClientException.cs" company="JP Dillingham">
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
    using System.Security.Permissions;

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

        protected SoulseekClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}