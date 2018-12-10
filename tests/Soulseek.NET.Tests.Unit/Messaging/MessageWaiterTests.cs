// <copyright file="MessageWaiterTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Messaging
{
    using Soulseek.NET.Messaging;
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;
    using static Soulseek.NET.Messaging.MessageWaiter;

    public class MessageWaiterTests
    {
        [Trait("Category", "Wait Completion")]
        [Fact(DisplayName = "Complete dequeues wait")]
        public async Task Complete_Dequeues_Wait()
        {
            using (var waiter = new MessageWaiter())
            {
                var result = Guid.NewGuid();
                var task = waiter.Wait<Guid>(new WaitKey(MessageCode.ServerLogin));
                waiter.Complete(MessageCode.ServerLogin, result);

                var waitResult = await task;

                var key = new WaitKey(MessageCode.ServerLogin);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                var peek = queue.TryPeek(out var wait);

                Assert.NotNull(queue);
                Assert.Empty(queue);
                Assert.False(peek);
            }
        }

        [Trait("Category", "Wait Completion")]
        [Fact(DisplayName = "Complete for missing wait does not throw")]
        public void Complete_For_Missing_Wait_Does_Not_Throw()
        {
            using (var waiter = new MessageWaiter())
            {
                var ex = Record.Exception(() => waiter.Complete<object>(MessageCode.ServerAddPrivilegedUser, null));

                Assert.Null(ex);
            }
        }

        [Trait("Category", "Wait Timeout")]
        [Fact(DisplayName = "Expiration ignores non-timed out waits")]
        public void Expiration_Ignores_Non_Timed_Out_Waits()
        {
            var key = new WaitKey(MessageCode.ServerLogin);

            using (var waiter = new MessageWaiter(0))
            {
                Task<object> task = waiter.Wait<object>(new WaitKey(key.MessageCode));
                Task<object> task2 = waiter.Wait<object>(new WaitKey(key.MessageCode), 30);
                object result = null;

                var ex = Record.Exception(() => result = task.Result);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                Assert.NotNull(ex);
                Assert.IsType<MessageTimeoutException>(ex.InnerException);
                Assert.Contains(MessageCode.ServerLogin.ToString(), ex.InnerException.Message);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Single(queue);
            }
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

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiate with empty constructor")]
        public void Instantiate_With_Empty_Constructor()
        {
            MessageWaiter t = null;
            var ex = Record.Exception(() => t = new MessageWaiter());

            var defaultConst = t.GetField<int>("DefaultTimeoutValue");

            Assert.Null(ex);
            Assert.NotNull(t);
            Assert.Equal(defaultConst, t.DefaultTimeout);

            t.Dispose();
        }

        [Trait("Category", "Wait Creation")]
        [Fact(DisplayName = "Wait for subsequent MessageCode enqueues Wait")]
        public void Wait_For_Subsequent_MessageCode_Enqueues_Wait()
        {
            using (var waiter = new MessageWaiter())
            {
                var task1 = waiter.Wait<object>(new WaitKey(MessageCode.ServerLogin));
                var task2 = waiter.Wait<object>(new WaitKey(MessageCode.ServerLogin));

                var key = new WaitKey(MessageCode.ServerLogin);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
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

        [Trait("Category", "Wait Creation")]
        [Theory(DisplayName = "Wait invocation creates valid Wait")]
        [InlineData(MessageCode.ServerLogin, null, null)]
        [InlineData(MessageCode.ServerLogin, "token", null)]
        [InlineData(MessageCode.ServerLogin, null, 13)]
        [InlineData(MessageCode.ServerLogin, "token", 13)]
        public void Wait_Invocation_Creates_Valid_Wait(MessageCode code, string token, int? timeout)
        {
            var key = new WaitKey(code, token);

            using (var waiter = new MessageWaiter())
            {
                Task<object> task = waiter.Wait<object>(new WaitKey(key.MessageCode, key.Token), timeout);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                Assert.IsType<Task<object>>(task);
                Assert.NotNull(task);
                Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Single(queue);
                Assert.NotEqual(new DateTime(), wait.DateTime);

                if (timeout != null)
                {
                    Assert.Equal(timeout, wait.TimeoutAfter);
                }
            }
        }

        [Trait("Category", "Wait Timeout")]
        [Fact(DisplayName = "Wait throws and is dequeued when timing out")]
        public void Wait_Throws_And_Is_Dequeued_When_Timing_out()
        {
            var key = new WaitKey(MessageCode.ServerLogin);

            using (var waiter = new MessageWaiter(0))
            {
                Task<object> task = waiter.Wait<object>(new WaitKey(key.MessageCode));
                object result = null;

                var ex = Record.Exception(() => result = task.Result);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                Assert.NotNull(ex);
                Assert.IsType<MessageTimeoutException>(ex.InnerException);
                Assert.Contains(MessageCode.ServerLogin.ToString(), ex.InnerException.Message);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Empty(queue);
            }
        }

        [Trait("Category", "Wait Cancellation")]
        [Fact(DisplayName = "Wait throws and is dequeued when cancelled")]
        public void Wait_Throws_And_Is_Dequeued_When_Cancelled()
        {
            var tcs = new CancellationTokenSource();
            tcs.CancelAfter(100);

            var key = new WaitKey(MessageCode.ServerLogin);

            using (var waiter = new MessageWaiter(0))
            {
                Task<object> task = waiter.Wait<object>(new WaitKey(key.MessageCode), 999999, tcs.Token);
                object result = null;

                var ex = Record.Exception(() => result = task.Result);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                Assert.NotNull(ex);
                Assert.IsType<MessageCancelledException>(ex.InnerException);
                Assert.Contains(MessageCode.ServerLogin.ToString(), ex.InnerException.Message);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Empty(queue);
            }
        }

        [Trait("Category", "Wait Cancellation")]
        [Fact(DisplayName = "All waits are cancelled when CancelAll is invoked")]
        public void All_Waits_Are_Cancelled_When_CancelAll_Is_Invoked()
        {
            var waiter = new MessageWaiter(0);
            var loginKey = new WaitKey(MessageCode.ServerLogin, "1");
            var loginKey2 = new WaitKey(MessageCode.ServerLogin, "2");
            var leaveKey = new WaitKey(MessageCode.ServerLeaveRoom);

            var loginTask = waiter.Wait<object>(new WaitKey(loginKey.MessageCode, loginKey.Token));
            var loginTask2 = waiter.Wait<object>(new WaitKey(loginKey2.MessageCode, loginKey2.Token));
            var leaveTask = waiter.Wait<object>(new WaitKey(leaveKey.MessageCode));

            waiter.CancelAll();

            Assert.True(loginTask.IsCanceled);
            Assert.True(loginTask2.IsCanceled);
            Assert.True(leaveTask.IsCanceled);
        }

        [Trait("Category", "Wait Throw")]
        [Fact(DisplayName = "Wait throws and is dequeued when thrown")]
        public void Wait_Throws_And_Is_Dequeued_When_Thrown()
        {
            var key = new WaitKey(MessageCode.ServerLogin);

            using (var waiter = new MessageWaiter(0))
            {
                Task<object> task = waiter.Wait<object>(new WaitKey(key.MessageCode), 999999);
                object result = null;

                waiter.Throw(key.MessageCode, new InvalidOperationException("error"));

                var ex = Record.Exception(() => result = task.Result);

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex.InnerException);
                Assert.Equal("error", ex.InnerException.Message);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Empty(queue);
            }
        }

        [Trait("Category", "Wait Creation")]
        [Fact(DisplayName = "WaitIndefinitely invocation creates Wait with max timeout")]
        public void WaitIndefinitely_Invocation_Creates_Wait_With_Max_Timeout()
        {
            var key = new WaitKey(MessageCode.ServerLogin);

            using (var waiter = new MessageWaiter())
            {
                var maxConst = waiter.GetField<int>("MaxTimeoutValue");

                Task<object> task = waiter.WaitIndefinitely<object>(new WaitKey(key.MessageCode, key.Token));

                var waits = waiter.GetProperty<ConcurrentDictionary<WaitKey, ConcurrentQueue<PendingWait>>>("Waits");
                waits.TryGetValue(key, out var queue);
                queue.TryPeek(out var wait);

                Assert.IsType<Task<object>>(task);
                Assert.NotNull(task);
                Assert.Equal(TaskStatus.WaitingForActivation, task.Status);

                Assert.NotEmpty(waits);
                Assert.Single(waits);

                Assert.NotNull(queue);
                Assert.Single(queue);
                Assert.NotEqual(new DateTime(), wait.DateTime);
                Assert.Equal(maxConst, wait.TimeoutAfter);
            }
        }
    }
}