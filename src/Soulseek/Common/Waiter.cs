// <copyright file="Waiter.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     Enables await-able server messages.
    /// </summary>
    internal class Waiter : IWaiter
    {
        private const int DefaultTimeoutValue = 5;
        private const int MaxTimeoutValue = 2147483647;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Waiter"/> class with the default timeout.
        /// </summary>
        internal Waiter()
            : this(DefaultTimeoutValue)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Waiter"/> class with the specified <paramref name="defaultTimeout"/>.
        /// </summary>
        /// <param name="defaultTimeout">The default timeout duration for message waits.</param>
        internal Waiter(int defaultTimeout)
        {
            DefaultTimeout = defaultTimeout;

            MonitorTimer = new SystemTimer()
            {
                Enabled = true,
                AutoReset = true,
                Interval = 500,
            };

            MonitorTimer.Elapsed += MonitorWaits;
        }

        /// <summary>
        ///     Gets the default timeout duration.
        /// </summary>
        public int DefaultTimeout { get; private set; }

        private bool Disposed { get; set; }
        private SystemTimer MonitorTimer { get; set; }
        private ConcurrentDictionary<WaitKey, ReaderWriterLockSlim> Locks { get; set; } = new ConcurrentDictionary<WaitKey, ReaderWriterLockSlim>();
        private ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>> Waits { get; set; } = new ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>();

        /// <summary>
        ///     Cancels all waits.
        /// </summary>
        public void CancelAll()
        {
            foreach (var record in Waits)
            {
                while (record.Value.TryDequeue(out var wait))
                {
                    wait.TaskCompletionSource.SetCanceled();
                }
            }
        }

        /// <summary>
        ///     Completes the oldest wait matching the specified <paramref name="key"/> with the specified <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="key">The unique WaitKey for the wait.</param>
        /// <param name="result">The wait result.</param>
        public void Complete<T>(WaitKey key, T result)
        {
            if (Waits.TryGetValue(key, out var queue) && queue.TryDequeue(out var wait))
            {
                ((TaskCompletionSource<T>)wait.TaskCompletionSource).SetResult(result);
            }
        }

        /// <summary>
        ///     Completes the oldest wait matching the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The unique WaitKey for the wait.</param>
        public void Complete(WaitKey key)
        {
            Complete<object>(key, null);
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Throws the specified <paramref name="exception"/> on the oldest wait matching the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The unique WaitKey for the wait.</param>
        /// <param name="exception">The Exception to throw.</param>
        public void Throw(WaitKey key, Exception exception)
        {
            if (Waits.TryGetValue(key, out var queue) && queue.TryDequeue(out var wait))
            {
                wait.TaskCompletionSource.SetException(exception);
            }
        }

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="key"/> and with the specified <paramref name="timeout"/>.
        /// </summary>
        /// <param name="key">A unique WaitKey for the wait.</param>
        /// <param name="timeout">The wait timeout.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        public Task Wait(WaitKey key, int? timeout = null, CancellationToken? cancellationToken = null)
        {
            return Wait<object>(key, timeout, cancellationToken);
        }

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="key"/> and with the specified <paramref name="timeout"/>.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="key">A unique WaitKey for the wait.</param>
        /// <param name="timeout">The wait timeout.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        public Task<T> Wait<T>(WaitKey key, int? timeout = null, CancellationToken? cancellationToken = null)
        {
            timeout = timeout ?? DefaultTimeout;

            var wait = new PendingWait()
            {
                TaskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously),
                DateTime = DateTime.UtcNow,
                TimeoutAfter = (int)timeout,
                CancellationToken = cancellationToken,
            };

            var recordLock = Locks.GetOrAdd(key, new ReaderWriterLockSlim());
            recordLock.EnterReadLock();

            try
            {
                Waits.AddOrUpdate(key, new ConcurrentQueue<PendingWait>(new[] { wait }), (_, queue) =>
                {
                    queue.Enqueue(wait);
                    return queue;
                });
            }
            finally
            {
                recordLock.ExitReadLock();
            }

            return ((TaskCompletionSource<T>)wait.TaskCompletionSource).Task;
        }

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="key"/> which does not time out.
        /// </summary>
        /// <param name="key">A unique WaitKey for the wait.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        public Task WaitIndefinitely(WaitKey key, CancellationToken? cancellationToken = null)
        {
            return WaitIndefinitely<object>(key, cancellationToken);
        }

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="key"/> which does not time out.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="key">A unique WaitKey for the wait.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        public Task<T> WaitIndefinitely<T>(WaitKey key, CancellationToken? cancellationToken = null)
        {
            return Wait<T>(key, MaxTimeoutValue, cancellationToken);
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        /// <param name="disposing">A value indicating whether disposal is in progress.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    MonitorTimer.Stop();
                    MonitorTimer.Dispose();

                    CancelAll();
                }

                Disposed = true;
            }
        }

        /// <remarks>
        ///     Not thread safe; ensure this is invoked only by the timer within this class.
        /// </remarks>
        private void MonitorWaits(object sender, object e)
        {
            foreach (var record in Waits)
            {
                // a lock should always be available or added prior to a wait; if not we'll take the null ref exception
                // that would follow. it should be impossible to hit this so a catastrophic failure is appropriate.
                Locks.TryGetValue(record.Key, out var recordLock);

                // enter a read lock first; TryPeek and TryDequeue are atomic so there's no risky operation until later.
                recordLock.EnterUpgradeableReadLock();

                try
                {
                    if (record.Value.TryPeek(out var nextPendingWait))
                    {
                        if (nextPendingWait.CancellationToken != null && ((CancellationToken)nextPendingWait.CancellationToken).IsCancellationRequested)
                        {
                            if (record.Value.TryDequeue(out var cancelledWait))
                            {
                                cancelledWait.TaskCompletionSource.SetException(new OperationCanceledException("The wait was cancelled."));
                            }
                        }
                        else if (nextPendingWait.DateTime.AddSeconds(nextPendingWait.TimeoutAfter) < DateTime.UtcNow && record.Value.TryDequeue(out var timedOutWait))
                        {
                            timedOutWait.TaskCompletionSource.SetException(new TimeoutException($"The wait timed out after {timedOutWait.TimeoutAfter} seconds."));
                        }
                    }

                    if (record.Value.IsEmpty)
                    {
                        // enter the write lock to prevent Wait() (which obtains a read lock) from enqueing any more waits
                        // before we can delete the dictionary record
                        recordLock.EnterWriteLock();

                        try
                        {
                            // check the queue again to ensure Wait() didn't enqueue anything between the last check and when we
                            // entered the write lock.  this is guarateed to be safe since we now have exclusive access to the record
                            if (record.Value.IsEmpty)
                            {
                                Waits.TryRemove(record.Key, out _);
                                Locks.TryRemove(record.Key, out _);
                            }
                        }
                        finally
                        {
                            recordLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    recordLock.ExitUpgradeableReadLock();
                }
            }
        }

        /// <summary>
        ///     The composite value for the wait dictionary.
        /// </summary>
        internal struct PendingWait
        {
            /// <summary>
            ///     The cancellation token for the wait.
            /// </summary>
            public CancellationToken? CancellationToken;

            /// <summary>
            ///     The time at which the wait was enqueued.
            /// </summary>
            public DateTime DateTime;

            /// <summary>
            ///     The task completion source for the wait task.
            /// </summary>
            public dynamic TaskCompletionSource;

            /// <summary>
            ///     The number of seconds after which the wait is to time out.
            /// </summary>
            public int TimeoutAfter;
        }
    }
}