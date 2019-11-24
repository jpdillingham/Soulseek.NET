// <copyright file="TransferTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Net;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Network.Tcp;
    using Xunit;

    public class TransferTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with the given data"), AutoData]
        internal void Instantiates_With_The_Given_Data(string username, string filename, int token)
        {
            var d = new Transfer(TransferDirection.Download, username, filename, token);

            Assert.Equal(username, d.Username);
            Assert.Equal(filename, d.Filename);
            Assert.Equal(token, d.Token);
        }

        [Trait("Category", "Properties")]
        [Theory(DisplayName = "Properties default to expected values"), AutoData]
        internal void Properties_Default_To_Expected_Values(string username, string filename, int token, TransferOptions options)
        {
            var d = new Transfer(TransferDirection.Download, username, filename, token, options);

            Assert.Null(d.Connection);
            Assert.Null(d.IPAddress);
            Assert.Null(d.Port);
            Assert.Null(d.Data);
            Assert.Null(d.RemoteToken);
            Assert.Equal(0, d.Size);
            Assert.Equal(TransferStates.None, d.State);
            Assert.Equal(0, d.AverageSpeed);
            Assert.Equal(0, d.BytesTransferred);
            Assert.Equal(0, d.BytesRemaining);
            Assert.Equal(default(TimeSpan), d.ElapsedTime);
            Assert.Equal(default(TimeSpan), d.RemainingTime);
            Assert.Null(d.StartTime);
            Assert.Null(d.EndTime);
            Assert.Equal(0, d.PercentComplete);
            Assert.Null(d.RemoteToken);
            Assert.Equal(options, d.Options);
        }

        [Trait("Category", "Properties")]
        [Theory(DisplayName = "IPAddress and Port props return Connection props"), AutoData]
        internal void IPAddress_And_Port_Props_Return_Connection_Props(string username, string filename, int token, IPAddress ipAddress, int port)
        {
            var d = new Transfer(TransferDirection.Download, username, filename, token);

            var c = new Mock<IConnection>();
            c.Setup(m => m.IPAddress)
                .Returns(ipAddress);
            c.Setup(m => m.Port)
                .Returns(port);

            d.Connection = c.Object;

            Assert.Equal(ipAddress, d.IPAddress);
            Assert.Equal(port, (int)d.Port);
        }

        [Trait("Category", "Wait Key")]
        [Theory(DisplayName = "Wait key is expected value"), AutoData]
        internal void Wait_Key_Is_Expected_Value(string username, string filename, int token, TransferDirection direction)
        {
            var d = new Transfer(direction, username, filename, token);

            Assert.Equal(new WaitKey(Constants.WaitKey.Transfer, direction, username, filename, token), d.WaitKey);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "State transitions set time props")]
        internal void State_Transitions_Set_Time_Props()
        {
            var d = new Transfer(TransferDirection.Download, string.Empty, string.Empty, 0);

            var s1 = d.StartTime;
            var e1 = d.EndTime;
            var et1 = d.ElapsedTime;
            var rt1 = d.RemainingTime;

            d.State = TransferStates.InProgress;

            var s2 = d.StartTime;
            var e2 = d.EndTime;
            var et2 = d.ElapsedTime;
            var rt2 = d.RemainingTime;

            d.State = TransferStates.Completed;

            Assert.Null(s1);
            Assert.Null(e1);
            Assert.Equal(default(TimeSpan), et1);
            Assert.Equal(default(TimeSpan), rt1);

            Assert.NotNull(s2);
            Assert.Null(e2);
            Assert.NotEqual(default(TimeSpan), et2);
            Assert.Equal(default(TimeSpan), rt2);

            Assert.NotNull(d.StartTime);
            Assert.NotNull(d.EndTime);
            Assert.NotEqual(default(TimeSpan), d.ElapsedTime);
            Assert.Equal(default(TimeSpan), d.RemainingTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "ElapsedTime works")]
        internal void ElapsedTime_Works()
        {
            var d = new Transfer(TransferDirection.Download, string.Empty, string.Empty, 0);

            var s = new DateTime(2019, 4, 25);
            var e = new DateTime(2019, 4, 26);

            Assert.Equal(default(TimeSpan), d.ElapsedTime);

            d.SetProperty("StartTime", s);

            Assert.NotEqual(default(TimeSpan), d.ElapsedTime);

            d.SetProperty("EndTime", e);

            Assert.Equal(e - s, d.ElapsedTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "RemainingTime works")]
        internal void RemainingTime_Works()
        {
            var d = new Transfer(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(default(TimeSpan), d.RemainingTime);

            d.SetProperty("AverageSpeed", 1);
            d.SetProperty("Size", 2);
            d.SetProperty("BytesTransferred", 1);

            Assert.Equal(TimeSpan.FromSeconds(1), d.RemainingTime);
        }

        [Trait("Category", "State")]
        [Fact(DisplayName = "PercentComplete works")]
        internal void PercentComplete_Works()
        {
            var d = new Transfer(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(0, d.PercentComplete);

            d.SetProperty("Size", 100);
            d.SetProperty("BytesTransferred", 50);

            Assert.Equal(50, d.PercentComplete);
        }

        [Trait("Category", "UpdateProgress")]
        [Fact(DisplayName = "UpdateProgress sets AverageSpeed initial value")]
        internal void UpdateProgress_Sets_AverageSpeed_To_Initial_Value()
        {
            var d = new Transfer(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(0, d.AverageSpeed);

            d.SetField("progressUpdateLimit", 0);
            d.SetProperty("State", TransferStates.InProgress);
            d.InvokeMethod("UpdateProgress", 100000);

            Assert.NotEqual(0, d.AverageSpeed);
        }

        [Trait("Category", "UpdateProgress")]
        [Fact(DisplayName = "UpdateProgress updates AverageSpeed on subsequent call")]
        internal void UpdateProgress_Updates_AverageSpeed_On_Subsequent_Call()
        {
            var d = new Transfer(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(0, d.AverageSpeed);

            d.SetField("progressUpdateLimit", 0);
            d.SetProperty("State", TransferStates.InProgress);
            d.InvokeMethod("UpdateProgress", 100000);

            var v1 = d.AverageSpeed;
            Assert.NotEqual(0, v1);

            d.InvokeMethod("UpdateProgress", 10);

            Assert.NotEqual(v1, d.AverageSpeed);
        }

        [Trait("Category", "UpdateProgress")]
        [Fact(DisplayName = "UpdateProgress ignores AverageSpeed if StartTime and lastProgressTime is null")]
        internal void UpdateProgress_Ignores_AverageSpeed_If_StarTime_And_lastProgressTime_Is_Null()
        {
            var d = new Transfer(TransferDirection.Download, string.Empty, string.Empty, 0);

            Assert.Equal(0, d.AverageSpeed);

            d.InvokeMethod("UpdateProgress", 100000);

            Assert.Equal(0, d.AverageSpeed);
        }
    }
}
