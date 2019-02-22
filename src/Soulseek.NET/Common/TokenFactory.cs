// <copyright file="TokenFactory.cs" company="JP Dillingham">
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
    ///     Generates unique tokens for network operations.
    /// </summary>
    public class TokenFactory : ITokenFactory
    {
        private readonly object syncLock = new object();
        private int current;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TokenFactory"/> class.
        /// </summary>
        /// <param name="start">The optional starting value.</param>
        public TokenFactory(int start = 0)
        {
            if (start == int.MaxValue)
            {
                throw new ArgumentException($"Invalid attempt to initialize the token factory starting value with the maximum integer value.", nameof(start));
            }

            current = start;
        }

        /// <summary>
        ///     Gets a new unique token.
        /// </summary>
        /// <returns>The new unique token.</returns>
        public int GetToken()
        {
            lock (syncLock)
            {
                if (current == int.MaxValue)
                {
                    current = 0;
                }

                return current++;
            }
        }
    }
}
