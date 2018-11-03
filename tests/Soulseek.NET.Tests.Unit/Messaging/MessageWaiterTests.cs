namespace Soulseek.NET.Tests.Unit.Messaging
{
    using Soulseek.NET.Messaging;
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Xunit;
    using static Soulseek.NET.Messaging.MessageWaiter;

    public class MessageWaiterTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiate with empty constructor")]
        public void Instantiate_With_Empty_Constructor()
        {
            MessageWaiter t = null;
            var ex = Record.Exception(() => t = new MessageWaiter());

            var defaultConst = t.GetNonPublicStaticField<int>("defaultTimeout");

            Assert.Null(ex);
            Assert.NotNull(t);
            Assert.Equal(defaultConst, t.DefaultTimeout);

            t.Dispose();
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiate with DefaultTimeout")]
        public void Instantiate_With_DefaultTimeout()
        {
            var timeout = new Random().Next();

            MessageWaiter t = null;
            var ex = Record.Exception(() => t = new MessageWaiter(timeout));

            Assert.Null(ex);
            Assert.NotNull(t);
            Assert.Equal(timeout, t.DefaultTimeout);

            t.Dispose();
        }

        [Trait("Category", "Wait Creation")]
        [Theory(DisplayName = "Wait invocation creates valid Wait")]
        [InlineData(MessageCode.ServerLogin, null, null)]
        [InlineData(MessageCode.ServerLogin, "token", null)]
        [InlineData(MessageCode.ServerLogin, null, 13)]
        [InlineData(MessageCode.ServerLogin, "token", 13)]
        public void Wait_Invocation_Creates_Valid_Wait(MessageCode code, object token, int? timeout)
        {
            var key = new WaitKey() { Code = code, Token = token };

            using (var waiter = new MessageWaiter())
            {
                Task<object> task = waiter.Wait<object>(key.Code, key.Token, timeout);

                var waits = waiter.GetNonPublicProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                Assert.IsType<Task<object>>(task);
                Assert.NotNull(task);
                Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Single(queue);

                Assert.NotNull(wait);
                Assert.NotEqual(new DateTime(), wait.DateTime);

                if (timeout != null)
                {
                    Assert.Equal(timeout, wait.TimeoutAfter);
                }
            }
        }

        [Trait("Category", "Wait Creation")]
        [Fact(DisplayName = "WaitIndefinitely invocation creates Wait with max timeout")]
        public void WaitIndefinitely_Invocation_Creates_Wait_With_Max_Timeout()
        {
            var key = new WaitKey() { Code = MessageCode.ServerLogin };

            using (var waiter = new MessageWaiter())
            {
                var maxConst = waiter.GetNonPublicStaticField<int>("maxTimeout");

                Task<object> task = waiter.WaitIndefinitely<object>(key.Code, key.Token);

                var waits = waiter.GetNonPublicProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                Assert.IsType<Task<object>>(task);
                Assert.NotNull(task);
                Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Single(queue);

                Assert.NotNull(wait);
                Assert.NotEqual(new DateTime(), wait.DateTime);
                Assert.Equal(maxConst, wait.TimeoutAfter);
            }
        }

        [Trait("Category", "Wait Creation")]
        [Fact(DisplayName = "Wait for subsequent MessageCode enqueues Wait")]
        public void Wait_For_Subsequent_MessageCode_Enqueues_Wait()
        {
            using (var waiter = new MessageWaiter())
            {
                var task1 = waiter.Wait<object>(MessageCode.ServerLogin);
                var task2 = waiter.Wait<object>(MessageCode.ServerLogin);

                var key = new WaitKey() { Code = MessageCode.ServerLogin };

                var waits = waiter.GetNonPublicProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);

                Assert.IsType<Task<object>>(task1);
                Assert.NotNull(task1);
                Assert.Equal(TaskStatus.WaitingForActivation, task1.Status);
                
                Assert.IsType<Task<object>>(task2);
                Assert.NotNull(task2);
                Assert.Equal(TaskStatus.WaitingForActivation, task2.Status);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Equal(2, queue.Count);
            }
        }
    }
}
