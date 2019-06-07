// <copyright file="PeerConnection_MessageReadTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Client
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Exceptions;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Soulseek.Messaging.Tcp;
    using Xunit;

    public class PeerConnection_MessageReadTests
    {
        [Trait("Category", "Diagnostic")]
        [Theory(DisplayName = "Creates diagnostic on message"), AutoData]
        public void Creates_Diagnostic_On_Message(string username, IPAddress ip, int port)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            List<string> messages = new List<string>();

            var diagnostic = new Mock<IDiagnosticFactory>();
            diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(msg));

            var message = new MessageBuilder()
                .Code(MessageCode.ServerParentMinSpeed)
                .WriteInteger(1)
                .Build();

            var s = new SoulseekClient("127.0.0.1", 1, diagnosticFactory: diagnostic.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, message);

            Assert.Contains(messages, m => m.IndexOf("peer message received", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Creates diagnostic on PeerUploadFailed message"), AutoData]
        public void Creates_Diagnostic_On_PeerUploadFailed_Message(string username, IPAddress ip, int port)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            List<string> messages = new List<string>();

            var diagnostic = new Mock<IDiagnosticFactory>();
            diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(msg => messages.Add(msg));

            var message = new MessageBuilder()
                .Code(MessageCode.PeerUploadFailed)
                .WriteString("foo")
                .Build();

            var s = new SoulseekClient("127.0.0.1", 1, diagnosticFactory: diagnostic.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, message);

            Assert.Contains(messages, m => m.IndexOf("peer message received", StringComparison.InvariantCultureIgnoreCase) > -1);
            Assert.Contains(messages, m => m.IndexOf("upload", StringComparison.InvariantCultureIgnoreCase) > -1 && m.IndexOf("failed", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Creates diagnostic on Exception"), AutoData]
        public void Creates_Diagnostic_On_Exception(string username, IPAddress ip, int port)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            List<string> messages = new List<string>();

            var diagnostic = new Mock<IDiagnosticFactory>();
            diagnostic.Setup(m => m.Warning(It.IsAny<string>(), It.IsAny<Exception>()))
                .Callback<string, Exception>((msg, ex) => messages.Add(msg));

            var message = new MessageBuilder()
                .Code(MessageCode.PeerTransferResponse)
                .Build(); // malformed message

            var s = new SoulseekClient("127.0.0.1", 1, diagnosticFactory: diagnostic.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, message);

            Assert.Contains(messages, m => m.IndexOf("error handling peer message", StringComparison.InvariantCultureIgnoreCase) > -1);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerTransferResponse"), AutoData]
        public void Completes_Wait_For_PeerTransferResponse(string username, IPAddress ip, int port, int token, bool allowed, int fileSize, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            var waiter = new Mock<IWaiter>();

            var msg = new PeerTransferResponse(token, allowed, fileSize, message).ToMessage();

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg);

            waiter.Verify(m => m.Complete(new WaitKey(MessageCode.PeerTransferResponse, username, token), It.Is<PeerTransferResponse>(r => r.Token == token)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerTransferRequest"), AutoData]
        public void Completes_Wait_For_PeerTransferRequest(string username, IPAddress ip, int port, int token, string filename)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            var waiter = new Mock<IWaiter>();

            var msg = new PeerTransferRequest(TransferDirection.Download, token, filename).ToMessage();

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg);

            waiter.Verify(m => m.Complete(new WaitKey(MessageCode.PeerTransferRequest, username, filename), It.Is<PeerTransferRequest>(r => r.Token == token)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerInfoResponse"), AutoData]
        public void Completes_Wait_For_PeerInfoResponse(string username, IPAddress ip, int port, string description, byte[] picture, int uploadSlots, int queueLength, bool hasFreeSlot)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            var waiter = new Mock<IWaiter>();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerInfoResponse)
                .WriteString(description)
                .WriteByte(1)
                .WriteInteger(picture.Length)
                .WriteBytes(picture)
                .WriteInteger(uploadSlots)
                .WriteInteger(queueLength)
                .WriteByte((byte)(hasFreeSlot ? 1 : 0))
                .Build();

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg);

            waiter.Verify(m => m.Complete(new WaitKey(MessageCode.PeerInfoResponse, username), It.IsAny<PeerInfoResponse>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerPlaceInQueueResponse"), AutoData]
        public void Completes_Wait_For_PeerPlaceInQueueResponse(string username, IPAddress ip, int port, string filename, int placeInQueue)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            var waiter = new Mock<IWaiter>();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerPlaceInQueueResponse)
                .WriteString(filename)
                .WriteInteger(placeInQueue)
                .Build();

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg);

            waiter.Verify(
                m => m.Complete(
                    new WaitKey(MessageCode.PeerPlaceInQueueResponse, username, filename),
                    It.Is<PeerPlaceInQueueResponse>(r => r.Filename == filename && r.PlaceInQueue == placeInQueue)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Completes wait for PeerBrowseResponse"), AutoData]
        public void Completes_Wait_For_PeerBrowseResponse(string username, IPAddress ip, int port, string directoryName)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            var waiter = new Mock<IWaiter>();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseResponse)
                .WriteInteger(1) // directory count
                .WriteString(directoryName) // first directory name
                .WriteInteger(0) // first directory file count
                .Compress()
                .Build();

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg);

            waiter.Verify(m => m.Complete(new WaitKey(MessageCode.PeerBrowseResponse, username), It.Is<BrowseResponse>(r => r.Directories.First().Directoryname == directoryName)), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Throws wait for PeerBrowseResponse given bad message"), AutoData]
        public void Throws_Wait_For_PeerBrowseResponse_Given_Bad_Message(string username, IPAddress ip, int port)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            var waiter = new Mock<IWaiter>();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerBrowseResponse)
                .Build();

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg);

            waiter.Verify(m => m.Throw(new WaitKey(MessageCode.PeerBrowseResponse, username), It.IsAny<MessageReadException>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Ignores inactive search response"), AutoData]
        public void Ignores_Inactive_Search_Response(string username, IPAddress ip, int port, int token, byte freeUploadSlots, int uploadSpeed, int queueLength)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            var waiter = new Mock<IWaiter>();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
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

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object);

            var ex = Record.Exception(() => s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg));

            Assert.Null(ex);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Appends active search response"), AutoData]
        public void Appends_Active_Search_Response(string username, IPAddress ip, int port, int token, byte freeUploadSlots, int uploadSpeed, int queueLength)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            var waiter = new Mock<IWaiter>();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerSearchResponse)
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

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object);

            var search = new Search("foo", token)
            {
                State = SearchStates.InProgress
            };

            var searches = new ConcurrentDictionary<int, Search>();
            searches.TryAdd(token, search);

            s.SetProperty("Searches", searches);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg);

            Assert.Single(search.Responses);
            Assert.Contains(search.Responses, r => r.Username == username && r.Token == token);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Throws PeerTransferRequest wait on PeerQueueFailed"), AutoData]
        public void Throws_PeerTransferRequest_Wait_On_PeerQueueFailed(string username, IPAddress ip, int port, string filename, string message)
        {
            var conn = new Mock<IMessageConnection>();
            conn.Setup(m => m.Username)
                .Returns(username);
            conn.Setup(m => m.IPAddress)
                .Returns(ip);
            conn.Setup(m => m.Port)
                .Returns(port);

            var waiter = new Mock<IWaiter>();

            var msg = new MessageBuilder()
                .Code(MessageCode.PeerQueueFailed)
                .WriteString(filename)
                .WriteString(message)
                .Build();

            var s = new SoulseekClient("127.0.0.1", 1, waiter: waiter.Object);

            s.InvokeMethod("PeerConnection_MessageRead", conn.Object, msg);

            waiter.Verify(m => m.Throw(new WaitKey(MessageCode.PeerTransferRequest, username, filename), It.IsAny<Exception>()), Times.Once);
        }
    }
}
