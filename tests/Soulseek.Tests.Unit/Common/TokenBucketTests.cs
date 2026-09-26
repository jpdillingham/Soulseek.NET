// <copyright file="TokenBucketTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Xunit;

    public class TokenBucketTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given 0 count")]
        public void Throws_ArgumentOutOfRangeException_Given_0_Count()
        {
            var ex = Record.Exception(() => new TokenBucket(0, 1000));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given negative count")]
        public void Throws_ArgumentOutOfRangeException_Given_Negative_Count()
        {
            var ex = Record.Exception(() => new TokenBucket(-1, 1000));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given 0 interval")]
        public void Throws_ArgumentOutOfRangeException_Given_0_Interval()
        {
            var ex = Record.Exception(() => new TokenBucket(1000, 0));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("interval", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given negative interval")]
        public void Throws_ArgumentOutOfRangeException_Given_Negative_Interval()
        {
            var ex = Record.Exception(() => new TokenBucket(1000, -1));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("interval", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Sets properties"), AutoData]
        public void Sets_Properties(int count, int interval)
        {
            using (var t = new TokenBucket(count, interval))
            {
                Assert.Equal(count, t.Capacity);
                Assert.Equal(interval, t.GetProperty<System.Timers.Timer>("Clock").Interval);
                Assert.Equal(count, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "SetCount")]
        [Fact(DisplayName = "SetCount throws ArgumentOutOfRangeException given 0 count")]
        public void SetCount_Throws_ArgumentOutOfRangeException_Given_0_Count()
        {
            using (var t = new TokenBucket(10, 1000))
            {
                var ex = Record.Exception(() => t.SetCapacity(0));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "SetCount")]
        [Fact(DisplayName = "SetCount throws ArgumentOutOfRangeException given negative count")]
        public void SetCount_Throws_ArgumentOutOfRangeException_Given_Negative_Count()
        {
            using (var t = new TokenBucket(10, 1000))
            {
                var ex = Record.Exception(() => t.SetCapacity(-1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("capacity", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "SetCapacity")]
        [Theory(DisplayName = "SetCapacity sets capacity"), AutoData]
        public void SetCapacity_Sets_Capacity(int count)
        {
            using (var t = new TokenBucket(10, 1000))
            {
                t.SetCapacity(count);

                Assert.Equal(count, t.Capacity);
            }
        }

        [Trait("Category", "SetCapacity")]
        [Fact(DisplayName = "SetCapacity lowers the available count immediately if the new capacity is lower")]
        public void SetCapacity_Lowers_The_Available_Count_Immediately_If_The_New_Capacity_Is_Lower()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.SetCapacity(10);

                Assert.Equal(10, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "SetCapacity")]
        [Fact(DisplayName = "SetCapacity lowers an overfilled count to the new capacity")]
        public void SetCapacity_Lowers_An_Overfilled_Count_To_The_New_Capacity()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.Return(100);

                t.SetCapacity(100);

                Assert.Equal(100, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "SetCapacity")]
        [Fact(DisplayName = "SetCapacity does not add tokens until the next reset if the new capacity is higher")]
        public void SetCapacity_Does_Not_Add_Tokens_Until_The_Next_Reset_If_The_New_Capacity_Is_Higher()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.SetCapacity(1000);

                Assert.Equal(100, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "SetCapacity")]
        [Fact(DisplayName = "SetCapacity fills the bucket to the new capacity on the next reset")]
        public async Task SetCapacity_Fills_The_Bucket_To_The_New_Capacity_On_The_Next_Reset()
        {
            using (var t = new TokenBucket(100, 50))
            {
                t.SetCapacity(1000);

                await Task.Delay(500);

                Assert.Equal(1000, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync decrements count by requested count")]
        public async Task GetAsync_Decrements_Count_By_Requested_Count()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync returns capacity if request exceeds capacity")]
        public async Task GetAsync_Returns_Capacity_If_Request_Exceeds_Capacity()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                int tokens = 0;
                var ex = await Record.ExceptionAsync(async() => tokens = await t.GetAsync(11));

                Assert.Null(ex);
                Assert.Equal(10, tokens);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync returns available tokens if request exceeds available count")]
        public async Task GetAsync_Returns_Available_Tokens_If_Request_Exceeds_Available_Count()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                await t.GetAsync(6);
                var count = await t.GetAsync(6);

                Assert.Equal(4, count);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync waits for reset if bucket is depleted")]
        public async Task GetAsync_Waits_For_Reset_If_Bucket_Is_Depleted()
        {
            using (var t = new TokenBucket(1, 10))
            {
                await t.GetAsync(1);
                await t.GetAsync(1);
                await t.GetAsync(1);

                Assert.True(true);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync with a negative count grants zero")]
        public async Task GetAsync_With_A_Negative_Count_Grants_Zero()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                var granted = await t.GetAsync(-5);

                Assert.Equal(0, granted);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync with a negative count does not change the available count")]
        public async Task GetAsync_With_A_Negative_Count_Does_Not_Change_The_Available_Count()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                await t.GetAsync(-5);

                Assert.Equal(100, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync returns no more than capacity if the bucket is overfilled")]
        public async Task GetAsync_Returns_No_More_Than_Capacity_If_The_Bucket_Is_Overfilled()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.Return(100);

                var granted = await t.GetAsync(150);

                Assert.Equal(100, granted);
                Assert.Equal(100, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync throws OperationCanceledException if cancelled while waiting for reset")]
        public async Task GetAsync_Throws_OperationCanceledException_If_Cancelled_While_Waiting_For_Reset()
        {
            using (var t = new TokenBucket(1, 100000))
            using (var cts = new CancellationTokenSource())
            {
                await t.GetAsync(1);

                var task = t.GetAsync(1, cts.Token);

                await cts.CancelAsync();

                var completed = await Task.WhenAny(task, Task.Delay(1000));

                Assert.Same(task, completed);
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync does not take tokens if cancelled while waiting for reset")]
        public async Task GetAsync_Does_Not_Take_Tokens_If_Cancelled_While_Waiting_For_Reset()
        {
            using (var t = new TokenBucket(1, 100000))
            using (var cts = new CancellationTokenSource())
            {
                await t.GetAsync(1);

                var task = t.GetAsync(1, cts.Token);

                await cts.CancelAsync();

                Assert.Same(task, await Task.WhenAny(task, Task.Delay(1000)));
                await Record.ExceptionAsync(() => task);

                // the next caller can still get the tokens that were returned, so the cancelled request released the semaphore
                t.Return(1);

                var next = t.GetAsync(1);
                var completed = await Task.WhenAny(next, Task.Delay(1000));

                Assert.Same(next, completed);
                Assert.Equal(1, await next);
            }
        }

        [Trait("Category", "GetAsync")]
        [Fact(DisplayName = "GetAsync does not change the bucket if cancelled while waiting for reset")]
        public async Task GetAsync_Does_Not_Change_The_Bucket_If_Cancelled_While_Waiting_For_Reset()
        {
            using (var t = new TokenBucket(1, 100000))
            using (var cts = new CancellationTokenSource())
            {
                await t.GetAsync(1);

                var reset = t.GetField<TaskCompletionSource<bool>>("waitForReset");

                var cancelled = t.GetAsync(1, cts.Token);
                var other = t.GetAsync(1);

                await cts.CancelAsync();
                await Record.ExceptionAsync(() => cancelled);

                // the other caller is still waiting for the timer to replenish the bucket
                var completed = await Task.WhenAny(other, Task.Delay(500));

                Assert.NotSame(other, completed);
                Assert.Equal(0, t.GetProperty<long>("CurrentCount"));
                Assert.Equal(1, t.Capacity);
                Assert.Same(reset, t.GetField<TaskCompletionSource<bool>>("waitForReset"));
                Assert.False(reset.Task.IsCompleted);
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return does not change count given negative")]
        public async Task Return_Does_Not_Change_Count_Given_Negative()
        {
            using (var t = new TokenBucket(10, 1000000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));

                t.Return(-5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return adds capacity given value larger than capacity")]
        public async Task Return_Adds_Capacity_Given_Value_Larger_Than_Capacity()
        {
            using (var t = new TokenBucket(10, 1000000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));

                t.Return(50);

                Assert.Equal(15, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return adds given value")]
        public async Task Return_Adds_Given_Value()
        {
            using (var t = new TokenBucket(10, 1000000))
            {
                await t.GetAsync(5);

                Assert.Equal(5, t.GetProperty<long>("CurrentCount"));

                t.Return(5);

                Assert.Equal(10, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return can overfill the bucket by up to 2x capacity")]
        public void Return_Can_Overfill_The_Bucket_By_Up_To_2x_Capacity()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                // returned tokens are added on top of a full bucket, allowing a burst
                t.Return(50);

                Assert.Equal(150, t.GetProperty<long>("CurrentCount"));

                // but the bucket never holds more than 2x capacity, no matter how many tokens are returned
                t.Return(100);
                t.Return(100);

                Assert.Equal(200, t.GetProperty<long>("CurrentCount"));
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Return allows a burst of no more than 2x capacity within a single interval")]
        public async Task Return_Allows_A_Burst_Of_No_More_Than_2x_Capacity_Within_A_Single_Interval()
        {
            using (var t = new TokenBucket(100, 100000))
            {
                t.Return(100);
                t.Return(100);

                var first = await t.GetAsync(100);
                var second = await t.GetAsync(100);

                // the burst is spent, so a third request must wait for the next interval
                var third = t.GetAsync(100);
                var completed = await Task.WhenAny(third, Task.Delay(250));

                Assert.Equal(100, first);
                Assert.Equal(100, second);
                Assert.NotSame(third, completed);
            }
        }

        [Trait("Category", "Return")]
        [Fact(DisplayName = "Reset discards an unused burst")]
        public async Task Reset_Discards_An_Unused_Burst()
        {
            using (var t = new TokenBucket(100, 50))
            {
                t.Return(100);

                await Task.Delay(500);

                Assert.Equal(100, t.GetProperty<long>("CurrentCount"));
            }
        }
    }
}
