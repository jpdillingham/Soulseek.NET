// <copyright file="HandleDownloadAsyncTests.cs" company="JP Dillingham">
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

using AutoFixture.Xunit2;
using Moq;
using Soulseek.NET.Messaging.Messages;
using Soulseek.NET.Tcp;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Soulseek.NET.Tests.Unit.Client
{
    public class HandleDownloadAsyncTests
    {
        [Trait("Category", "HandleDownloadAsync")]
        [Fact(DisplayName = "Does not throw on download missing from ActiveDownloads")]
        public async Task Does_Not_Throw_On_Download_Missing_From_ActiveDownloads()
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(1)));

            var s = new SoulseekClient("127.0.0.1", 1, null);
            var r = new ConnectToPeerResponse("username", "F", IPAddress.Parse("127.0.0.1"), 1, 1);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("HandleDownloadAsync", r, conn.Object));

            Assert.Null(ex);
        }

        [Trait("Category", "HandleDownloadAsync")]
        [Theory(DisplayName = "Completes 'start' wait when download exists"), AutoData]
        public async Task Completes_Start_Wait_When_Download_Exists(string username, string filename, int token, int remoteToken)
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(remoteToken)));

            var waiter = new Mock<IWaiter>();

            var s = new SoulseekClient("127.0.0.1", 1, null, messageWaiter: waiter.Object);

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            var download = new Download(username, filename, token);
            activeDownloads.TryAdd(remoteToken, download);

            s.SetProperty("ActiveDownloads", activeDownloads);

            var r = new ConnectToPeerResponse(username, "F", IPAddress.Parse("127.0.0.1"), 1, token);

            await s.InvokeMethod<Task>("HandleDownloadAsync", r, conn.Object);

            waiter.Verify(m => m.Complete(download.WaitKey), Times.Once);
        }

        [Trait("Category", "HandleDownloadAsync")]
        [Theory(DisplayName = "Does not throw on remote token read Exception"), AutoData]
        public async Task Does_Not_Throw_On_Remote_Token_Read_Exception(string username, string filename, int token, int remoteToken)
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Returns(Task.FromException<byte[]>(new Exception()));

            var waiter = new Mock<IWaiter>();

            var s = new SoulseekClient("127.0.0.1", 1, null, messageWaiter: waiter.Object);

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            var download = new Download(username, filename, token);
            activeDownloads.TryAdd(remoteToken, download);

            s.SetProperty("ActiveDownloads", activeDownloads);

            var r = new ConnectToPeerResponse(username, "F", IPAddress.Parse("127.0.0.1"), 1, token);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("HandleDownloadAsync", r, conn.Object));

            Assert.Null(ex);
            waiter.Verify(m => m.Complete(download.WaitKey), Times.Never);
        }

        [Trait("Category", "HandleDownloadAsync")]
        [Theory(DisplayName = "Transfer timeout disconnects and does not throw"), AutoData]
        public async Task Transfer_Timeout_Disconnects_And_Does_Not_Throw(string username, string filename, int token, int remoteToken)
        {
            var reads = 0;
            string message = null;

            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Callback<int>(c => reads++)
                .Returns(() =>
                {
                    return reads == 1 ?
                        Task.FromResult(BitConverter.GetBytes(remoteToken)) :
                        Task.FromException<byte[]>(new TimeoutException());
                });
            conn.Setup(m => m.Disconnect(It.IsAny<string>()))
                .Callback<string>(str => message = str);

            var waiter = new Mock<IWaiter>();

            var s = new SoulseekClient("127.0.0.1", 1, null, messageWaiter: waiter.Object);

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            var download = new Download(username, filename, token);
            activeDownloads.TryAdd(remoteToken, download);

            s.SetProperty("ActiveDownloads", activeDownloads);

            var r = new ConnectToPeerResponse(username, "F", IPAddress.Parse("127.0.0.1"), 1, token);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("HandleDownloadAsync", r, conn.Object));

            Assert.Null(ex);
            conn.Verify(m => m.Disconnect(It.IsAny<string>()), Times.Once);
            Assert.Contains("timed out", message);
        }

        [Trait("Category", "HandleDownloadAsync")]
        [Theory(DisplayName = "Transfer exception disconnects and does not throw"), AutoData]
        public async Task Transfer_Exception_Disconnects_And_Does_Not_Throw(string username, string filename, int token, int remoteToken)
        {
            var reads = 0;
            string message = null;

            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Callback<int>(c => reads++)
                .Returns(() =>
                {
                    return reads == 1 ?
                        Task.FromResult(BitConverter.GetBytes(remoteToken)) :
                        Task.FromException<byte[]>(new Exception("fake exception"));
                });
            conn.Setup(m => m.Disconnect(It.IsAny<string>()))
                .Callback<string>(str => message = str);

            var waiter = new Mock<IWaiter>();

            var s = new SoulseekClient("127.0.0.1", 1, null, messageWaiter: waiter.Object);

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            var download = new Download(username, filename, token);
            activeDownloads.TryAdd(remoteToken, download);

            s.SetProperty("ActiveDownloads", activeDownloads);

            var r = new ConnectToPeerResponse(username, "F", IPAddress.Parse("127.0.0.1"), 1, token);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("HandleDownloadAsync", r, conn.Object));

            Assert.Null(ex);
            conn.Verify(m => m.Disconnect(It.IsAny<string>()), Times.Once);
            Assert.Contains("fake exception", message);
        }

        [Trait("Category", "HandleDownloadAsync")]
        [Theory(DisplayName = "Raises DownloadProgressUpdated event on data read"), AutoData]
        public async Task Raises_DownloadProgressUpdated_Event_On_Data_Read(string username, string filename, int token, int remoteToken, int bytesDownloaded)
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(remoteToken)))
                .Raises(m => m.DataRead += null, this, new ConnectionDataEventArgs(new byte[0], bytesDownloaded, 1));

            DownloadProgressUpdatedEventArgs e = null;

            var s = new SoulseekClient("127.0.0.1", 1, null);
            s.DownloadProgressUpdated += (sender, args) => { e = args; };

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            var download = new Download(username, filename, token);
            activeDownloads.TryAdd(remoteToken, download);

            s.SetProperty("ActiveDownloads", activeDownloads);

            var r = new ConnectToPeerResponse(username, "F", IPAddress.Parse("127.0.0.1"), 1, token);

            await s.InvokeMethod<Task>("HandleDownloadAsync", r, conn.Object);

            Assert.NotNull(e);
            Assert.Equal(bytesDownloaded, e.BytesDownloaded);
        }

        [Trait("Category", "HandleDownloadAsync")]
        [Theory(DisplayName = "Completes download wait with expected data on successful transfer"), AutoData]
        public async Task Completes_Download_Wait_With_Expected_Data_On_Successful_Transfer(string username, string filename, int token, int remoteToken, byte[] data)
        {
            var reads = 0;

            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Callback<int>(c => reads++)
                .Returns(() =>
                {
                    return reads == 1 ?
                        Task.FromResult(BitConverter.GetBytes(remoteToken)) :
                        Task.FromResult(data);
                });
            conn.Setup(m => m.Disconnect(It.IsAny<string>()))
                .Raises(m => m.Disconnected += null, this, string.Empty);

            var waiter = new Mock<IWaiter>();

            var s = new SoulseekClient("127.0.0.1", 1, null, messageWaiter: waiter.Object);

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            var download = new Download(username, filename, token);
            activeDownloads.TryAdd(remoteToken, download);

            s.SetProperty("ActiveDownloads", activeDownloads);

            var r = new ConnectToPeerResponse(username, "F", IPAddress.Parse("127.0.0.1"), 1, token);

            await s.InvokeMethod<Task>("HandleDownloadAsync", r, conn.Object);

            waiter.Verify(m => m.Complete(download.WaitKey, data), Times.Once);
        }
    }
}
