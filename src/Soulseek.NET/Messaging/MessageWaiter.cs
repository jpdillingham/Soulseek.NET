namespace Soulseek.NET.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class MessageWaiter
    {
        private ReaderWriterLockSlim Lock { get; set; } = new ReaderWriterLockSlim();
        private Dictionary<object, Queue<TaskCompletionSource<object>>> Waits { get; set; } = new Dictionary<object, Queue<TaskCompletionSource<object>>>();

        public void Complete(MessageCode code, object result)
        {
            Complete(code, null, result);
        }

        public void Complete(MessageCode code, object token, object result)
        {
            var key = GetKey(code, token);

            Lock.EnterUpgradeableReadLock();

            try
            {
                if (Waits.ContainsKey(key))
                {
                    var queue = Waits[key];

                    if (queue.Count > 0)
                    {
                        Lock.EnterWriteLock();

                        try
                        {
                            var wait = queue.Dequeue();
                            wait.SetResult(result);

                            queue.TrimExcess();
                        }
                        finally
                        {
                            Lock.ExitWriteLock();
                        }
                    }
                }
            }
            finally
            {
                Lock.ExitUpgradeableReadLock();
            }
        }

        public TaskCompletionSource<object> Wait(MessageCode code, object token = null)
        {
            var key = GetKey(code, token);
            var wait = new TaskCompletionSource<object>();

            Lock.EnterWriteLock();

            try
            {
                if (Waits.ContainsKey(key))
                {
                    Waits[key].Enqueue(wait);
                }
                else
                {
                    Waits.Add(key, new Queue<TaskCompletionSource<object>>(new[] { wait }));
                }
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            return wait;
        }

        private object GetKey(MessageCode code, object token)
        {
            return token == null ? code : (object)new Tuple<MessageCode, object>(code, token);
        }
    }
}