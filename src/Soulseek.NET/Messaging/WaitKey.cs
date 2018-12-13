// <copyright file="WaitKey.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Messaging
{
    using System;

    /// <summary>
    ///     The composite key for the wait dictionary.
    /// </summary>
    internal class WaitKey
    {
        public WaitKey(params object[] tokenParts)
        {
            TokenParts = tokenParts;
        }

        public object[] TokenParts { get; private set; }

        /// <summary>
        ///     The wait token.
        /// </summary>
        public string Token => string.Join(":", TokenParts);

        public override bool Equals(object obj)
        {
            try
            {
                var key = (WaitKey)obj;
                return Token == key.Token;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Token.GetHashCode();
        }
    }
}
