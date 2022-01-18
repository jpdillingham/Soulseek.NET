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
            Assert.Equal("count", ((ArgumentOutOfRangeException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws ArgumentOutOfRangeException given negative count")]
        public void Throws_ArgumentOutOfRangeException_Given_Negative_Count()
        {
            var ex = Record.Exception(() => new TokenBucket(-1, 1000));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Equal("count", ((ArgumentOutOfRangeException)ex).ParamName);
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
                Assert.Equal(count, t.GetProperty<int>("Count"));
                Assert.Equal(interval, t.GetProperty<System.Timers.Timer>("Clock").Interval);
                Assert.Equal(count, t.GetProperty<int>("CurrentCount"));
            }
        }

        [Trait("Category", "SetCount")]
        [Fact(DisplayName = "SetCount throws ArgumentOutOfRangeException given 0 count")]
        public void SetCount_Throws_ArgumentOutOfRangeException_Given_0_Count()
        {
            using (var t = new TokenBucket(10, 1000))
            {
                var ex = Record.Exception(() => t.SetCount(0));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("count", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "SetCount")]
        [Fact(DisplayName = "SetCount throws ArgumentOutOfRangeException given negative count")]
        public void SetCount_Throws_ArgumentOutOfRangeException_Given_Negative_Count()
        {
            using (var t = new TokenBucket(10, 1000))
            {
                var ex = Record.Exception(() => t.SetCount(-1));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("count", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "SetCount")]
        [Theory(DisplayName = "SetCount sets count"), AutoData]
        public void SetCount_Sets_Count(int count)
        {
            using (var t = new TokenBucket(10, 1000))
            {
                t.SetCount(count);

                Assert.Equal(count, t.GetProperty<int>("Count"));
            }
        }

        [Trait("Category", "WaitAsync")]
        [Fact(DisplayName = "WaitAsync decrements count by 1")]
        public async Task WaitAsync_Decrements_Count_By_1()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                await t.WaitAsync();

                Assert.Equal(9, t.GetProperty<int>("CurrentCount"));
            }
        }

        [Trait("Category", "WaitAsync")]
        [Fact(DisplayName = "WaitAsync decrements count by requested count")]
        public async Task WaitAsync_Decrements_Count_By_Requested_Count()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                await t.WaitAsync(5);

                Assert.Equal(5, t.GetProperty<int>("CurrentCount"));
            }
        }

        [Trait("Category", "WaitAsync")]
        [Fact(DisplayName = "WaitAsync throws ArgumentOutOfRangeException if requested count exceeds capacity")]
        public async Task WaitAsync_Throws_ArgumentOutOfRangeException_If_Requested_Count_Exceeds_Capacity()
        {
            using (var t = new TokenBucket(10, 10000))
            {
                var ex = await Record.ExceptionAsync(() => t.WaitAsync(11));

                Assert.NotNull(ex);
                Assert.IsType<ArgumentOutOfRangeException>(ex);
                Assert.Equal("count", ((ArgumentOutOfRangeException)ex).ParamName);
            }
        }

        [Trait("Category", "WaitAsync")]
        [Fact(DisplayName = "WaitAsync waits for reset if bucket is depleted")]
        public async Task WaitAsync_Waits_For_Reset_If_Bucket_Is_Depleted()
        {
            using (var t = new TokenBucket(1, 10))
            {
                await t.WaitAsync();
                await t.WaitAsync();

                Assert.Equal(0, t.GetProperty<int>("CurrentCount"));
            }
        }
    }
}
