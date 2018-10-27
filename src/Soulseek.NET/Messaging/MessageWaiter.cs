namespace Soulseek.NET.Messaging
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    public class MessageWaiter
    {
        private ConcurrentDictionary<object, ConcurrentQueue<TaskCompletionSource<object>>> Waits { get; set; } = new ConcurrentDictionary<object, ConcurrentQueue<TaskCompletionSource<object>>>();

        public void Complete(MessageCode code, object result)
        {
            Complete(code, null, result);
        }

        public void Complete(MessageCode code, object token, object result)
        {
            var key = GetKey(code, token);

            if (Waits.TryGetValue(key, out var queue))
            {
                if (queue.TryDequeue(out var wait))
                {
                    wait.SetResult(result);
                }
            }
        }

        public TaskCompletionSource<object> Wait(MessageCode code, object token = null)
        {
            var key = GetKey(code, token);
            var wait = new TaskCompletionSource<object>();

            Waits.AddOrUpdate(key, new ConcurrentQueue<TaskCompletionSource<object>>(new[] { wait }), (_, queue) =>
            {
                queue.Enqueue(wait);
                return queue;
            });

            return wait;
        }

        private object GetKey(MessageCode code, object token)
        {
            return token == null ? code : (object)new Tuple<MessageCode, object>(code, token);
        }
    }
}