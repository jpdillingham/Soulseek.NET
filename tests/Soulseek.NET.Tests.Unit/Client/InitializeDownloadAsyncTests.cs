// <copyright file="InitializeDownloadAsyncTests.cs" company="JP Dillingham">
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

namespace Soulseek.NET.Tests.Unit.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Net;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.NET.Messaging.Messages;
    using Soulseek.NET.Tcp;
    using Xunit;

    public class InitializeDownloadAsyncTests
    {
        [Trait("Category", "InitializeDownloadAsync")]
        [Fact(DisplayName = "Does not throw on download missing from ActiveDownloads")]
        public async Task Does_Not_Throw_On_Download_Missing_From_ActiveDownloads()
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(1)));

            var connFactory = new Mock<IConnectionFactory>();
            connFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var s = new SoulseekClient("127.0.0.1", 1, null, connectionFactory: connFactory.Object);
            var r = new ConnectToPeerResponse("username", "F", IPAddress.Parse("127.0.0.1"), 1, 1);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("InitializeDownloadAsync", r));

            Assert.Null(ex);
        }

        [Trait("Category", "InitializeDownloadAsync")]
        [Theory(DisplayName = "Completes 'start' wait when download exists"), AutoData]
        public async Task Completes_Start_Wait_When_Download_Exists(string username, string filename, int token, int remoteToken)
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Returns(Task.FromResult(BitConverter.GetBytes(remoteToken)));

            var connFactory = new Mock<IConnectionFactory>();
            connFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var waiter = new Mock<IWaiter>();

            var s = new SoulseekClient("127.0.0.1", 1, null, messageWaiter: waiter.Object, connectionFactory: connFactory.Object);

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            var download = new Download(username, filename, token);
            activeDownloads.TryAdd(remoteToken, download);

            s.SetProperty("ActiveDownloads", activeDownloads);

            var r = new ConnectToPeerResponse(username, "F", IPAddress.Parse("127.0.0.1"), 1, token);

            await s.InvokeMethod<Task>("InitializeDownloadAsync", r);

            waiter.Verify(m => m.Complete(download.WaitKey, It.IsAny<IConnection>()), Times.Once);
        }

        [Trait("Category", "InitializeDownloadAsync")]
        [Theory(DisplayName = "Does not throw on remote token read Exception"), AutoData]
        public async Task Does_Not_Throw_On_Remote_Token_Read_Exception(string username, string filename, int token, int remoteToken)
        {
            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Returns(Task.FromException<byte[]>(new Exception()));

            var connFactory = new Mock<IConnectionFactory>();
            connFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var waiter = new Mock<IWaiter>();

            var s = new SoulseekClient("127.0.0.1", 1, null, messageWaiter: waiter.Object, connectionFactory: connFactory.Object);

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            var download = new Download(username, filename, token);
            activeDownloads.TryAdd(remoteToken, download);

            s.SetProperty("ActiveDownloads", activeDownloads);

            var r = new ConnectToPeerResponse(username, "F", IPAddress.Parse("127.0.0.1"), 1, token);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("InitializeDownloadAsync", r));

            Assert.Null(ex);
            waiter.Verify(m => m.Complete(download.WaitKey), Times.Never);
        }

        [Trait("Category", "InitializeDownloadAsync")]
        [Theory(DisplayName = "Transfer exception disconnects and does not throw"), AutoData]
        public async Task Transfer_Exception_Disconnects_And_Does_Not_Throw(string username, string filename, int token, int remoteToken)
        {
            string message = null;

            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(It.IsAny<int>()))
                .Returns(Task.FromException<byte[]>(new Exception("fake exception")));
            conn.Setup(m => m.Disconnect(It.IsAny<string>()))
                .Callback<string>(str => message = str);

            var connFactory = new Mock<IConnectionFactory>();
            connFactory.Setup(m => m.GetConnection(It.IsAny<IPAddress>(), It.IsAny<int>(), It.IsAny<ConnectionOptions>()))
                .Returns(conn.Object);

            var waiter = new Mock<IWaiter>();

            var s = new SoulseekClient("127.0.0.1", 1, null, messageWaiter: waiter.Object, connectionFactory: connFactory.Object);

            var activeDownloads = new ConcurrentDictionary<int, Download>();
            var download = new Download(username, filename, token);
            activeDownloads.TryAdd(remoteToken, download);

            s.SetProperty("ActiveDownloads", activeDownloads);

            var r = new ConnectToPeerResponse(username, "F", IPAddress.Parse("127.0.0.1"), 1, token);

            var ex = await Record.ExceptionAsync(async () => await s.InvokeMethod<Task>("InitializeDownloadAsync", r));

            Assert.Null(ex);
            conn.Verify(m => m.Disconnect(It.IsAny<string>()), Times.Once);
            Assert.Contains("fake exception", message, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
