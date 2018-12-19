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

namespace Soulseek.NET
{
    using System;

    /// <summary>
    ///     Uniquely identifies a Wait.
    /// </summary>
    internal class WaitKey : IEquatable<WaitKey>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="WaitKey"/> class.
        /// </summary>
        /// <param name="tokenParts">The parts which make up the key.</param>
        public WaitKey(params object[] tokenParts)
        {
            TokenParts = tokenParts;
        }

        /// <summary>
        ///     Gets the wait token.
        /// </summary>
        public string Token => string.Join(":", TokenParts);

        /// <summary>
        ///     Gets the parts which make up the key.
        /// </summary>
        public object[] TokenParts { get; private set; }

        /// <summary>
        ///     Compares the specified <paramref name="obj"/> to this instance.
        /// </summary>
        /// <param name="obj">The object to which to compare.</param>
        /// <returns>A value indicating whether the specified object is equal to this instance.</returns>
        public override bool Equals(object obj)
        {
            try
            {
                return Equals((WaitKey)obj);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Compares the specified <paramref name="waitKey"/> to this instance.
        /// </summary>
        /// <param name="waitKey">The WaitKey to which to compare.</param>
        /// <returns>A value indicating whether the specified WaitKey is equal to this instance.</returns>
        public bool Equals(WaitKey waitKey)
        {
            return Token == waitKey.Token;
        }

        /// <summary>
        ///     Returns the hash code of this instance.
        /// </summary>
        /// <returns>The hash code of this instance.</returns>
        public override int GetHashCode()
        {
            return Token == string.Empty ? 0 : Token.GetHashCode();
        }
    }
}