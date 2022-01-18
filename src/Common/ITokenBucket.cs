// <copyright file="ITokenBucket.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Implements the 'token bucket' or 'leaky bucket' rate limiting algorithm.
    /// </summary>
    internal interface ITokenBucket
    {
        /// <summary>
        ///     Sets the token count to the supplied <paramref name="count"/>.
        /// </summary>
        /// <remarks>Change takes effect on the next reset.</remarks>
        /// <param name="count">The new number of tokens.</param>
        void SetCount(int count);

        /// <summary>
        ///     Asynchronously waits for a single token from the bucket.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that completes when the token has been provided.</returns>
        Task WaitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///     Asynchronously waits for the requested token <paramref name="count"/> from the bucket.
        /// </summary>
        /// <param name="count">The number of tokens for which to wait.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that completes when the requested number of tokens have been provided.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the requested number of tokens exceeds the bucket capacity.
        /// </exception>
        Task WaitAsync(int count, CancellationToken cancellationToken = default);
    }
}