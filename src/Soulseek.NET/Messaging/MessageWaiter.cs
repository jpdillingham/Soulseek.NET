// <copyright file="SoulseekClient.cs" company="JP Dillingham">
//     Copyright(C) 2018 JP Dillingham
//     
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//     
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
//     GNU General Public License for more details.
//     
//     You should have received a copy of the GNU General Public License
//     along with this program.If not, see<https://www.gnu.org/licenses/>.
// </copyright>

namespace Soulseek.NET.Messaging
{
    using Soulseek.NET.Common;
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using SystemTimer = System.Timers.Timer;

    /// <summary>
    ///     Enables await-able server messages.
    /// </summary>
    internal class MessageWaiter
    {
        private const int defaultTimeout = 5;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageWaiter"/> class with the default timeout.
        /// </summary>
        internal MessageWaiter()
            : this(defaultTimeout)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageWaiter"/> class with the specified <paramref name="defaultTimeout"/>.
        /// </summary>
        /// <param name="defaultTimeout">The default timeout duration for message waits.</param>
        internal MessageWaiter(int defaultTimeout)
        {
            DefaultTimeout = defaultTimeout;

            TimeoutTimer = new SystemTimer()
            {
                Enabled = true,
                AutoReset = true,
                Interval = 500,
            };

            TimeoutTimer.Elapsed += CompleteExpiredWaits;
        }

        private int DefaultTimeout { get; set; }
        private SystemTimer TimeoutTimer { get; set; }
        private ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>> Waits { get; set; } = new ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>();

        internal void Complete(MessageCode code, object result)
        {
            Complete(code, null, result);
        }

        internal void Complete(MessageCode code, object token, object result)
        {
            var key = GetKey(code, token);

            if (Waits.TryGetValue(key, out var queue))
            {
                if (queue.TryDequeue(out var wait))
                {
                    wait.TaskCompletionSource.SetResult(result);
                }
            }
        }

        internal TaskCompletionSource<object> Wait(MessageCode code)
        {
            return Wait(code, null, DefaultTimeout);
        }

        internal TaskCompletionSource<object> Wait(MessageCode code, int timeout)
        {
            return Wait(code, null, timeout);
        }

        internal TaskCompletionSource<object> Wait(MessageCode code, object token)
        {
            return Wait(code, token, DefaultTimeout);
        }

        internal TaskCompletionSource<object> Wait(MessageCode code, object token, int timeout)
        {
            var key = GetKey(code, token);

            var wait = new PendingWait()
            {
                TaskCompletionSource = new TaskCompletionSource<object>(),
                DateTime = DateTime.UtcNow,
                TimeoutAfter = timeout,
            };

            Waits.AddOrUpdate(key, new ConcurrentQueue<PendingWait>(new[] { wait }), (_, queue) =>
            {
                queue.Enqueue(wait);
                return queue;
            });

            return wait.TaskCompletionSource;
        }

        internal TaskCompletionSource<object> WaitIndefinitely(MessageCode code)
        {
            return Wait(code, null, 2147483647);
        }

        internal TaskCompletionSource<object> WaitIndefinitely(MessageCode code, object token)
        {
            return Wait(code, token, 2147483647);
        }

        private void CompleteExpiredWaits(object sender, object e)
        {
            foreach (var queue in Waits)
            {
                if (queue.Value.TryPeek(out var nextPendingWait) && nextPendingWait.DateTime.AddSeconds(nextPendingWait.TimeoutAfter) < DateTime.UtcNow)
                {
                    if (queue.Value.TryDequeue(out var timedOutWait))
                    {
                        timedOutWait.TaskCompletionSource.SetException(new MessageTimeoutException($"Message wait for {queue.Key.Code} ({queue.Key.Token}) timed out after {timedOutWait.TimeoutAfter} seconds."));
                    }
                }
            }
        }

        private WaitKey GetKey(MessageCode code, object token)
        {
            return new WaitKey() { Code = code, Token = token };
        }

        private struct WaitKey
        {
            public MessageCode Code;
            public object Token;
        }

        private class PendingWait
        {
            public DateTime DateTime { get; set; }
            public TaskCompletionSource<object> TaskCompletionSource { get; set; }
            public int TimeoutAfter { get; set; }
        }
    }
}