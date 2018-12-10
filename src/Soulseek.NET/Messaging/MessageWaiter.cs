// <copyright file="MessageWaiter.cs" company="JP Dillingham">
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
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     Enables await-able server messages.
    /// </summary>
    internal class MessageWaiter : IMessageWaiter
    {
        private const int DefaultTimeoutValue = 5;
        private const int MaxTimeoutValue = 2147483647;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageWaiter"/> class with the default timeout.
        /// </summary>
        internal MessageWaiter()
            : this(DefaultTimeoutValue)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageWaiter"/> class with the specified <paramref name="defaultTimeout"/>.
        /// </summary>
        /// <param name="defaultTimeout">The default timeout duration for message waits.</param>
        internal MessageWaiter(int defaultTimeout)
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
        private ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>> Waits { get; set; } = new ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>();

        /// <summary>
        ///     Cancels all waits.
        /// </summary>
        public void CancelAll()
        {
            foreach (var queue in Waits)
            {
                while (queue.Value.TryDequeue(out var wait))
                {
                    wait.TaskCompletionSource.SetCanceled();
                }
            }
        }

        /// <summary>
        ///     Completes the oldest wait matching the specified <paramref name="messageCode"/> with the specified <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="messageCode">The wait message code.</param>
        /// <param name="result">The wait result.</param>
        public void Complete<T>(MessageCode messageCode, T result)
        {
            Complete(messageCode, null, result);
        }

        /// <summary>
        ///     Completes the oldest wait matching the specified <paramref name="messageCode"/> and <paramref name="token"/> with
        ///     the specified <paramref name="result"/>.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="messageCode">The wait message code.</param>
        /// <param name="token">The unique wait token.</param>
        /// <param name="result">The wait result.</param>
        public void Complete<T>(MessageCode messageCode, string token, T result)
        {
            var key = new WaitKey(messageCode, token);

            if (Waits.TryGetValue(key, out var queue))
            {
                if (queue.TryDequeue(out var wait))
                {
                    ((TaskCompletionSource<T>)wait.TaskCompletionSource).SetResult(result);
                }
            }
        }

        /// <summary>
        ///     Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        ///     Throws the specified <paramref name="exception"/> on the oldest wait matching the specified <paramref name="messageCode"/>.
        /// </summary>
        /// <param name="messageCode">The wait message code.</param>
        /// <param name="exception">The Exception to throw.</param>
        public void Throw(MessageCode messageCode, Exception exception)
        {
            Throw(messageCode, null, exception);
        }

        /// <summary>
        ///     Throws the specified <paramref name="exception"/> on the oldest wait matching the specified
        ///     <paramref name="messageCode"/> and <paramref name="token"/>.
        /// </summary>
        /// <param name="messageCode">The wait message code.</param>
        /// <param name="token">The unique wait token.</param>
        /// <param name="exception">The Exception to throw.</param>
        public void Throw(MessageCode messageCode, string token, Exception exception)
        {
            var key = new WaitKey(messageCode, token);

            if (Waits.TryGetValue(key, out var queue))
            {
                if (queue.TryDequeue(out var wait))
                {
                    wait.TaskCompletionSource.SetException(exception);
                }
            }
        }

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="messageCode"/> and <paramref name="token"/> and with the
        ///     specified <paramref name="timeout"/>.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="messageCode">The wait message code.</param>
        /// <param name="token">A unique token for the wait.</param>
        /// <param name="timeout">The wait timeout.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        public Task<T> Wait<T>(MessageCode messageCode, string token = null, int? timeout = null, CancellationToken? cancellationToken = null)
        {
            timeout = timeout ?? DefaultTimeout;

            var key = new WaitKey(messageCode, token);

            var wait = new PendingWait()
            {
                TaskCompletionSource = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously),
                DateTime = DateTime.UtcNow,
                TimeoutAfter = (int)timeout,
                CancellationToken = cancellationToken,
            };

            Waits.AddOrUpdate(key, new ConcurrentQueue<PendingWait>(new[] { wait }), (_, queue) =>
            {
                queue.Enqueue(wait);
                return queue;
            });

            return ((TaskCompletionSource<T>)wait.TaskCompletionSource).Task;
        }

        /// <summary>
        ///     Adds a new wait for the specified <paramref name="messageCode"/> which does not time out.
        /// </summary>
        /// <typeparam name="T">The wait result type.</typeparam>
        /// <param name="messageCode">The wait message code.</param>
        /// <param name="token">A unique token for the wait.</param>
        /// <param name="cancellationToken">The cancellation token for the wait.</param>
        /// <returns>A Task representing the wait.</returns>
        public Task<T> WaitIndefinitely<T>(MessageCode messageCode, string token = null, CancellationToken? cancellationToken = null)
        {
            return Wait<T>(messageCode, token, MaxTimeoutValue, cancellationToken);
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

        private void MonitorWaits(object sender, object e)
        {
            foreach (var queue in Waits)
            {
                if (queue.Value.TryPeek(out var nextPendingWait))
                {
                    var wait = queue.Key.MessageCode + (queue.Key.Token == null ? string.Empty : $" ({queue.Key.Token}) ");

                    if (nextPendingWait.CancellationToken != null && ((CancellationToken)nextPendingWait.CancellationToken).IsCancellationRequested)
                    {
                        if (queue.Value.TryDequeue(out var cancelledWait))
                        {
                            var message = $"Message wait for {wait} was cancelled.";
                            cancelledWait.TaskCompletionSource.SetException(new MessageCancelledException(message));
                        }
                    }
                    else if (nextPendingWait.DateTime.AddSeconds(nextPendingWait.TimeoutAfter) < DateTime.UtcNow)
                    {
                        if (queue.Value.TryDequeue(out var timedOutWait))
                        {
                            var message = $"Message wait for {wait} timed out after {timedOutWait.TimeoutAfter} seconds.";
                            timedOutWait.TaskCompletionSource.SetException(new MessageTimeoutException(message));
                        }
                    }
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