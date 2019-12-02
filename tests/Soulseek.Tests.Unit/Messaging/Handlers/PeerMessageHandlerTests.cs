// <copyright file="PeerMessageHandlerTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Handlers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Xunit;

    public class PeerMessageHandlerTests
    {
        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on message"), AutoData]
        public void Creates_Diagnostic_On_Message(string username, IPAddress ip, int port)
        {
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, ip, port);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(msg));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.ParentMinSpeed)
                .WriteInteger(1)
                .Build();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("peer message received", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Creates diagnostic on PeerUploadFailed message"), AutoData]
        public void Creates_Diagnostic_On_PeerUploadFailed_Message(string username, IPAddress ip, int port)
        {
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, ip, port);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(msg));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadFailed)
                .WriteString("foo")
                .Build();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("peer message received", StringComparison.InvariantCultureIgnoreCase) > -1);
            Assert.Contains(messages, m => m.IndexOf("upload", StringComparison.InvariantCultureIgnoreCase) > -1 && m.IndexOf("failed", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Throws TransferRequest wait on PeerUploadFailed message"), AutoData]
        public void Throws_TransferRequest_Wait_On_PeerUploadFailed_Message(string username, IPAddress ip, int port, string filename)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var dict = new ConcurrentDictionary<int, TransferInternal>();
            dict.TryAdd(0, new TransferInternal(TransferDirection.Download, username, filename, 0));

            mocks.Client.Setup(m => m.Downloads)
                .Returns(dict);

            mocks.PeerConnection.Setup(m => m.Username)
                .Returns(username);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadFailed)
                .WriteString(filename)
                .Build();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            mocks.Waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.IsAny<TransferException>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Throws download wait on PeerUploadFailed message"), AutoData]
        public void Throws_Download_Wait_On_PeerUploadFailed_Message(string username, IPAddress ip, int port, string filename)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var dict = new ConcurrentDictionary<int, TransferInternal>();
            var download = new TransferInternal(TransferDirection.Download, username, filename, 0);
            dict.TryAdd(0, download);

            mocks.Client.Setup(m => m.Downloads)
                .Returns(dict);

            mocks.PeerConnection.Setup(m => m.Username)
                .Returns(username);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.UploadFailed)
                .WriteString(filename)
                .Build();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            mocks.Waiter.Verify(m => m.Throw(download.WaitKey, It.IsAny<TransferException>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Creates diagnostic on Exception"), AutoData]
        public void Creates_Diagnostic_On_Exception(string username, IPAddress ip, int port)
        {
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, ip, port);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Peer.TransferResponse)
                .Build(); // malformed message

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("error handling peer message", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for TransferResponse"), AutoData]
        public void Completes_Wait_For_TransferResponse(string username, IPAddress ip, int port, int token, int fileSize)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var msg = new TransferResponse(token, fileSize).ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferResponse, username, token), It.Is<TransferResponse>(r => r.Token == token)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerInfoResponse"), AutoData]
        public void Completes_Wait_For_PeerInfoResponse(string username, IPAddress ip, int port, string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.InfoResponse)
                .WriteString(description)
                .WriteByte(1)
                .WriteInteger(picture.Length)
                .WriteBytes(picture)
                .WriteInteger(uploadSlots)
                .WriteInteger(queueLength)
                .WriteByte((byte)(hasFreeSlot ? 1 : 0))
                .Build();

            handler.HandleMessage(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.InfoResponse, username), It.IsAny<UserInfo>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerPlaceInQueueResponse"), AutoData]
        public void Completes_Wait_For_PeerPlaceInQueueResponse(string username, IPAddress ip, int port, string filename, int placeInQueue)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.PlaceInQueueResponse)
                .WriteString(filename)
                .WriteInteger(placeInQueue)
                .Build();

            handler.HandleMessage(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(
                m => m.Complete(
                    new WaitKey(MessageCode.Peer.PlaceInQueueResponse, username, filename),
                    It.Is<PlaceInQueueResponse>(r => r.Filename == filename && r.PlaceInQueue == placeInQueue)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerBrowseResponse"), AutoData]
        public void Completes_Wait_For_PeerBrowseResponse(string username, IPAddress ip, int port, string directoryName)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .WriteInteger(1) // directory count
                .WriteString(directoryName) // first directory name
                .WriteInteger(0) // first directory file count
                .Compress()
                .Build();

            handler.HandleMessage(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.BrowseResponse, username), It.Is<BrowseResponse>(r => r.Directories.First().Directoryname == directoryName)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Throws wait for PeerBrowseResponse given bad message"), AutoData]
        public void Throws_Wait_For_PeerBrowseResponse_Given_Bad_Message(string username, IPAddress ip, int port)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.BrowseResponse)
                .Build();

            handler.HandleMessage(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Peer.BrowseResponse, username), It.IsAny<MessageReadException>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Ignores inactive search response"), AutoData]
        public void Ignores_Inactive_Search_Response(string username, IPAddress ip, int port, int token, byte freeUploadSlots, int uploadSpeed, int queueLength)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var ex = Record.Exception(() => handler.HandleMessage(mocks.PeerConnection.Object, msg));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Throws TransferRequest wait on PeerQueueFailed"), AutoData]
        public void Throws_TransferRequest_Wait_On_PeerQueueFailed(string username, IPAddress ip, int port, string filename, string message)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.QueueFailed)
                .WriteString(filename)
                .WriteString(message)
                .Build();

            handler.HandleMessage(mocks.PeerConnection.Object, msg);

            mocks.Waiter.Verify(m => m.Throw(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.IsAny<Exception>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Appends active search response"), AutoData]
        public void Appends_Active_Search_Response(string username, IPAddress ip, int port, int token, byte freeUploadSlots, int uploadSpeed, int queueLength)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Peer.SearchResponse)
                .WriteString(username)
                .WriteInteger(token)
                .WriteInteger(1) // file count
                .WriteByte(0x2) // code
                .WriteString("filename") // filename
                .WriteLong(3) // size
                .WriteString("ext") // extension
                .WriteInteger(1) // attribute count
                .WriteInteger((int)FileAttributeType.BitDepth) // attribute[0].type
                .WriteInteger(4) // attribute[0].value
                .WriteByte(freeUploadSlots)
                .WriteInteger(uploadSpeed)
                .WriteLong(queueLength)
                .WriteBytes(new byte[4]) // unknown 4 bytes
                .Compress()
                .Build();

            var responses = new List<SearchResponse>();

            using (var search = new SearchInternal("foo", token)
            {
                State = SearchStates.InProgress,
                ResponseReceived = (r) => responses.Add(r),
            })
            {
                mocks.Searches.TryAdd(token, search);

                handler.HandleMessage(mocks.PeerConnection.Object, msg);

                Assert.Single(responses);
                Assert.Contains(responses, r => r.Username == username && r.Token == token);
            }
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Sends default UserInfoResponse if resolver throws")]
        public async Task Sends_Default_UserInfoResponse_If_Resolver_Throws()
        {
            var options = new SoulseekClientOptions(userInfoResponseResolver: (u, i, p) => throw new Exception());

            var defaultResponse = await new SoulseekClientOptions()
                .UserInfoResponseResolver(null, null, 0).ConfigureAwait(false);
            var defaultMessage = Encoding.UTF8.GetString(defaultResponse.ToByteArray());

            var (handler, mocks) = GetFixture(options: options);

            var msg = new UserInfoRequest().ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, msg);

            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == defaultMessage), null), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Sends resolved UserInfoResponse"), AutoData]
        public void Sends_Resolved_UserInfoResponse(string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeUploadSlot)
        {
            var response = new UserInfo(description, picture, uploadSlots, queueLength, hasFreeUploadSlot);
            var options = new SoulseekClientOptions(userInfoResponseResolver: (u, i, p) => Task.FromResult(response));

            var (handler, mocks) = GetFixture(options: options);

            var msg = new UserInfoRequest().ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, msg);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on failed UserInfoResponse resolution"), AutoData]
        public void Creates_Diagnostic_On_Failed_UserInfoResponse_Resolution(string username, IPAddress ip, int port)
        {
            var options = new SoulseekClientOptions(userInfoResponseResolver: (u, i, p) => throw new Exception());
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, ip, port, options);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new UserInfoRequest().ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("Failed to resolve UserInfoResponse", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Sends resolved BrowseResponse")]
        public void Sends_Resolved_BrowseResponse()
        {
            var files = new List<File>()
            {
                new File(1, "1", 1, "1", 1, new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitDepth, 1) }),
                new File(2, "2", 2, "2", 1, new List<FileAttribute>() { new FileAttribute(FileAttributeType.BitRate, 2) }),
            };

            IEnumerable<Directory> dirs = new List<Directory>()
            {
                new Directory("1", 2, files),
                new Directory("2", 2, files),
            };

            var response = new BrowseResponse(2, dirs);
            var options = new SoulseekClientOptions(browseResponseResolver: (u, i, p) => Task.FromResult(dirs));

            var (handler, mocks) = GetFixture(options: options);

            var msg = new BrowseRequest().ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, msg);

            mocks.PeerConnection.Verify(
                m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(response.ToByteArray())), null), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on failed BrowseResponse resolution"), AutoData]
        public void Creates_Diagnostic_On_Failed_BrowseResponse_Resolution(string username, IPAddress ip, int port)
        {
            var options = new SoulseekClientOptions(browseResponseResolver: (u, i, p) => throw new Exception());
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, ip, port, options);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new BrowseRequest().ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("Failed to resolve BrowseResponse", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on failed QueueDownload invocation via QueueDownload"), AutoData]
        public void Creates_Diagnostic_On_Failed_QueueDownload_Invocation_Via_QueueDownload(string username, IPAddress ip, int port, string filename)
        {
            var options = new SoulseekClientOptions(queueDownloadAction: (u, f, i, p) => throw new Exception());
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, ip, port, options);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new QueueDownloadRequest(filename).ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("Failed to invoke QueueDownload action", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on failed QueueDownload invocation via TransferRequest"), AutoData]
        public void Creates_Diagnostic_On_Failed_QueueDownload_Invocation_Via_TransferRequest(string username, IPAddress ip, int port, int token, string filename)
        {
            var options = new SoulseekClientOptions(queueDownloadAction: (u, f, i, p) => throw new Exception());
            List<string> messages = new List<string>();

            var (handler, mocks) = GetFixture(username, ip, port, options);

            mocks.Diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            Assert.Contains(messages, m => m.IndexOf("Failed to invoke QueueDownload action", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Writes TransferResponse on successful QueueDownload invocation"), AutoData]
        public void Writes_TransferResponse_On_Successful_QueueDownload_Invocation(string username, IPAddress ip, int port, int token, string filename)
        {
            var options = new SoulseekClientOptions(queueDownloadAction: (u, f, i, p) => Task.CompletedTask);
            var (handler, mocks) = GetFixture(username, ip, port, options);

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
            var expected = new TransferResponse(token, "Queued.").ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(expected)), null), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Writes TransferResponse and QueueFailedResponse on failed QueueDownload invocation"), AutoData]
        public void Writes_TransferResponse_And_QueueFailedResponse_On_Failed_QueueDownload_Invocation(string username, IPAddress ip, int port, int token, string filename)
        {
            var options = new SoulseekClientOptions(queueDownloadAction: (u, f, i, p) => throw new Exception());
            var (handler, mocks) = GetFixture(username, ip, port, options);

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
            var expectedTransferResponse = new TransferResponse(token, "Enqueue failed due to internal error.").ToByteArray();
            var expectedQueueFailedResponse = new QueueFailedResponse(filename, "Enqueue failed due to internal error.").ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(expectedTransferResponse)), null), Times.Once);
            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(expectedQueueFailedResponse)), null), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Writes TransferResponse and QueueFailedResponse on rejected QueueDownload invocation"), AutoData]
        public void Writes_TransferResponse_And_QueueFailedResponse_On_Rejected_QueueDownload_Invocation(string username, IPAddress ip, int port, int token, string filename, string rejectMessage)
        {
            var options = new SoulseekClientOptions(queueDownloadAction: (u, f, i, p) => throw new QueueDownloadException(rejectMessage));
            var (handler, mocks) = GetFixture(username, ip, port, options);

            var message = new TransferRequest(TransferDirection.Download, token, filename).ToByteArray();
            var expectedTransferResponse = new TransferResponse(token, rejectMessage).ToByteArray();
            var expectedQueueFailedResponse = new QueueFailedResponse(filename, rejectMessage).ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(expectedTransferResponse)), null), Times.Once);
            mocks.PeerConnection.Verify(m => m.WriteAsync(It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == Encoding.UTF8.GetString(expectedQueueFailedResponse)), null), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Completes TransferRequest wait on upload request"), AutoData]
        public void Completes_TransferRequest_Wait_On_Upload_Request(string username, IPAddress ip, int port, int token, string filename)
        {
            var (handler, mocks) = GetFixture(username, ip, port);

            var request = new TransferRequest(TransferDirection.Upload, token, filename);
            var message = request.ToByteArray();

            handler.HandleMessage(mocks.PeerConnection.Object, message);

            mocks.Waiter.Verify(m => m.Complete(new WaitKey(MessageCode.Peer.TransferRequest, username, filename), It.Is<TransferRequest>(t => t.Direction == request.Direction && t.Token == request.Token && t.Filename == request.Filename)), Times.Once);
        }

        private (PeerMessageHandler Handler, Mocks Mocks) GetFixture(string username = null, IPAddress ip = null, int port = 0, SoulseekClientOptions options = null)
        {
            var mocks = new Mocks(options);

            mocks.ServerConnection.Setup(m => m.Username)
                .Returns(username ?? "username");
            mocks.ServerConnection.Setup(m => m.IPAddress)
                .Returns(ip ?? IPAddress.Parse("0.0.0.0"));
            mocks.ServerConnection.Setup(m => m.Port)
                .Returns(port);

            mocks.PeerConnection.Setup(m => m.Username)
                .Returns(username ?? "username");
            mocks.PeerConnection.Setup(m => m.IPAddress)
                .Returns(ip ?? IPAddress.Parse("0.0.0.0"));
            mocks.PeerConnection.Setup(m => m.Port)
                .Returns(port);

            var handler = new PeerMessageHandler(
                mocks.Client.Object,
                mocks.Diagnostic.Object);

            return (handler, mocks);
        }

        private class Mocks
        {
            public Mocks(SoulseekClientOptions clientOptions = null)
            {
                Client = new Mock<SoulseekClient>(clientOptions);
                Client.CallBase = true;

                Client.Setup(m => m.Waiter).Returns(Waiter.Object);
                Client.Setup(m => m.Downloads).Returns(Downloads);
                Client.Setup(m => m.Searches).Returns(Searches);
                Client.Setup(m => m.ServerConnection).Returns(ServerConnection.Object);
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public ConcurrentDictionary<int, TransferInternal> Downloads { get; } = new ConcurrentDictionary<int, TransferInternal>();
            public ConcurrentDictionary<int, SearchInternal> Searches { get; } = new ConcurrentDictionary<int, SearchInternal>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IMessageConnection> PeerConnection { get; } = new Mock<IMessageConnection>();
        }
    }
}
