// <copyright file="TokenBucket.cs" company="JP Dillingham">
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
    internal sealed class TokenBucket
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TokenBucket"/> class.
        /// </summary>
        /// <param name="count">The initial number of tokens.</param>
        /// <param name="interval">The interval at which tokens are replenished.</param>
        public TokenBucket(int count, int interval)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to 1");
            }

            if (interval < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than or equal to 1");
            }

            Count = count;
            CurrentCount = Count;

            Clock = new System.Timers.Timer(interval);
            Clock.Elapsed += (sender, e) => _ = Reset();
            Clock.Start();
        }

        private System.Timers.Timer Clock { get; set; }
        private int Count { get; set; }
        private int CurrentCount { get; set; }
        private SemaphoreSlim SyncRoot { get; } = new SemaphoreSlim(1, 1);
        private TaskCompletionSource<bool> WaitForReset { get; set; } = new TaskCompletionSource<bool>();

        /// <summary>
        ///     Sets the token count to the supplied <paramref name="count"/>.
        /// </summary>
        /// <remarks>Change takes effect on the next reset.</remarks>
        /// <param name="count">The new number of tokens.</param>
        public void SetCount(int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than or equal to 1");
            }

            Count = count;
        }

        /// <summary>
        ///     Asynchronously waits for a single token from the bucket.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that completes when the token has been provided.</returns>
        public Task WaitAsync(CancellationToken cancellationToken = default)
            => WaitAsync(1, cancellationToken);

        /// <summary>
        ///     Asynchronously waits for the requested token <paramref name="count"/> from the bucket.
        /// </summary>
        /// <param name="count">The number of tokens for which to wait.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A Task that completes when the requested number of tokens have been provided.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the requested number of tokens exceeds the bucket capacity.
        /// </exception>
        public Task WaitAsync(int count, CancellationToken cancellationToken = default)
        {
            if (count > Count)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, $"Requested count exceeds token count of {Count}");
            }

            return WaitInternalAsync(count, cancellationToken);
        }

        private async Task Reset()
        {
            await SyncRoot.WaitAsync().ConfigureAwait(false);

            try
            {
                CurrentCount = Count;

                WaitForReset.SetResult(true);
                WaitForReset = new TaskCompletionSource<bool>();
            }
            finally
            {
                SyncRoot.Release();
            }
        }

        private async Task WaitInternalAsync(int count, CancellationToken cancellationToken = default)
        {
            Task waitTask = Task.CompletedTask;

            await SyncRoot.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (CurrentCount >= count)
                {
                    CurrentCount -= count;
                    return;
                }

                waitTask = WaitForReset.Task;
            }
            finally
            {
                SyncRoot.Release();
            }

            await waitTask.ConfigureAwait(false);
        }
    }
}