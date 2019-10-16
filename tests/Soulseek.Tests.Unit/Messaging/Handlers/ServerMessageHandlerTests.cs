// <copyright file="ServerMessageHandlerTests.cs" company="JP Dillingham">
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
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Handlers;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;
    using Soulseek.Network.Tcp;
    using Soulseek.Options;
    using Xunit;

    public class ServerMessageHandlerTests
    {
        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Creates diagnostic on message")]
        public void Creates_Diagnostic_On_Message()
        {
            var (handler, mocks) = GetFixture();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.ParentMinSpeed)
                .WriteInteger(1)
                .Build();

            handler.HandleMessage(null, message);

            mocks.Diagnostic.Verify(m => m.Debug(It.IsAny<string>()), Times.Once);
        }

        [Trait("Category", "Diagnostic")]
        [Fact(DisplayName = "Creates unhandled diagnostic on unhandled message")]
        public void Creates_Unhandled_Diagnostic_On_Unhandled_Message()
        {
            string msg = null;
            var (handler, mocks) = GetFixture();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()))
                .Callback<string>(m => msg = m);

            var message = new MessageBuilder().WriteCode(MessageCode.Server.PrivateRoomOwned).Build();

            handler.HandleMessage(null, message);

            mocks.Diagnostic.Verify(m => m.Debug(It.IsAny<string>()), Times.Exactly(2));

            Assert.Contains("Unhandled", msg, StringComparison.InvariantCultureIgnoreCase);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerGetPeerAddress"), AutoData]
        public void Handles_ServerGetPeerAddress(string username, IPAddress ip, int port)
        {
            GetPeerAddressResponse result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<GetPeerAddressResponse>()))
                .Callback<WaitKey, GetPeerAddressResponse>((key, response) => result = response);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetPeerAddress)
                .WriteString(username)
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .Build();

            handler.HandleMessage(null, message);

            Assert.Equal(username, result.Username);
            Assert.Equal(ip, result.IPAddress);
            Assert.Equal(port, result.Port);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises PrivateMessageReceived event on ServerPrivateMessage"), AutoData]
        public void Raises_PrivateMessageRecieved_Event_On_ServerPrivateMessage(int id, int timeOffset, string username, string message, bool isAdmin)
        {
            var options = new ClientOptions(autoAcknowledgePrivateMessages: false);
            var (handler, mocks) = GetFixture(options);

            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = epoch.AddSeconds(timeOffset).ToLocalTime();

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateMessage)
                .WriteInteger(id)
                .WriteInteger(timeOffset)
                .WriteString(username)
                .WriteString(message)
                .WriteByte((byte)(isAdmin ? 1 : 0))
                .Build();

            PrivateMessage response = null;
            handler.PrivateMessageReceived += (_, privateMessage) => response = privateMessage;

            handler.HandleMessage(null, msg);

            Assert.NotNull(response);
            Assert.Equal(id, response.Id);
            Assert.Equal(timestamp, response.Timestamp);
            Assert.Equal(username, response.Username);
            Assert.Equal(message, response.Message);
            Assert.Equal(isAdmin, response.IsAdmin);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Acknowledges ServerPrivateMessage"), AutoData]
        internal void Acknowledges_ServerPrivateMessage(int id, int timeOffset, string username, string message, bool isAdmin)
        {
            var options = new ClientOptions(autoAcknowledgePrivateMessages: true);
            var (handler, mocks) = GetFixture(options);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivateMessage)
                .WriteInteger(id)
                .WriteInteger(timeOffset)
                .WriteString(username)
                .WriteString(message)
                .WriteByte((byte)(isAdmin ? 1 : 0))
                .Build();

            handler.HandleMessage(null, msg);

            mocks.Client.Verify(m =>
                m.AcknowledgePrivateMessageAsync(id, It.IsAny<CancellationToken>()));
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles IntegerResponse messages")]
        [InlineData(MessageCode.Server.ParentMinSpeed)]
        [InlineData(MessageCode.Server.ParentSpeedRatio)]
        [InlineData(MessageCode.Server.WishlistInterval)]
        internal void Handles_IntegerResponse_Messages(MessageCode.Server code)
        {
            int value = new Random().Next();
            int? result = null;

            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<int>()))
                .Callback<WaitKey, int>((key, response) => result = response);

            var msg = new MessageBuilder()
                .WriteCode(code)
                .WriteInteger(value)
                .Build();

            handler.HandleMessage(null, msg);

            Assert.Equal(value, result);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerLogin"), AutoData]
        public void Handles_ServerLogin(bool success, string message, IPAddress ip)
        {
            LoginResponse result = null;

            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<LoginResponse>()))
                .Callback<WaitKey, LoginResponse>((key, response) => result = response);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.Login)
                .WriteByte((byte)(success ? 1 : 0))
                .WriteString(message)
                .WriteBytes(ipBytes)
                .Build();

            handler.HandleMessage(null, msg);

            Assert.Equal(success, result.Succeeded);
            Assert.Equal(message, result.Message);
            Assert.Equal(ip, result.IPAddress);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerRoomList"), AutoData]
        public void Handles_ServerRoomList(List<(string Name, int UserCount)> rooms)
        {
            IReadOnlyCollection<(string Name, int UserCount)> result = null;

            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<IReadOnlyCollection<(string Name, int UserCount)>>()))
                .Callback<WaitKey, IReadOnlyCollection<(string Name, int UserCount)>>((key, response) => result = response);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.RoomList)
                .WriteInteger(rooms.Count);

            rooms.ForEach(room => builder.WriteString(room.Name));
            builder.WriteInteger(rooms.Count);
            rooms.ForEach(room => builder.WriteInteger(room.UserCount));

            handler.HandleMessage(null, builder.Build());

            foreach (var room in rooms)
            {
                Assert.Contains(result, r => r.Name == room.Name);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerPrivilegedUsers"), AutoData]
        public void Handles_ServerPrivilegedUsers(string[] names)
        {
            IReadOnlyCollection<string> result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<IReadOnlyCollection<string>>()))
                .Callback<WaitKey, IReadOnlyCollection<string>>((key, response) => result = response);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.PrivilegedUsers)
                .WriteInteger(names.Length);

            foreach (var name in names)
            {
                builder.WriteString(name);
            }

            var msg = builder.Build();

            handler.HandleMessage(null, msg);

            foreach (var name in names)
            {
                Assert.Contains(result, n => n == name);
            }
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Creates connection on ConnectToPeerResponse 'P'"), AutoData]
        public void Creates_Connection_On_ConnectToPeerResponse_P(string username, int token, IPAddress ip, int port)
        {
            ConnectToPeerResponse response = null;
            var (handler, mocks) = GetFixture();

            mocks.PeerConnectionManager
                .Setup(m => m.GetOrAddMessageConnectionAsync(It.IsAny<ConnectToPeerResponse>()))
                .Returns(Task.FromResult(new Mock<IMessageConnection>().Object))
                .Callback<ConnectToPeerResponse>((r) => response = r);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("P")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .Build();

            handler.HandleMessage(null, msg);

            Assert.Equal(username, response.Username);
            Assert.Equal(ip, response.IPAddress);
            Assert.Equal(port, response.Port);

            mocks.PeerConnectionManager.Verify(m => m.GetOrAddMessageConnectionAsync(It.IsAny<ConnectToPeerResponse>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Ignores ConnectToPeerResponse 'F' on unexpected connection"), AutoData]
        public void Ignores_ConnectToPeerResponse_F_On_Unexpected_Connection(string username, int token, IPAddress ip, int port)
        {
            var (handler, mocks) = GetFixture();

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("F")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .Build();

            var ex = Record.Exception(() => handler.HandleMessage(null, msg));

            Assert.Null(ex);
            Assert.Empty(mocks.Downloads);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on ignored ConnectToPeerResponse 'F'"), AutoData]
        public void Raises_DiagnosticGenerated_On_Ignored_ConnectToPeerResponse_F(string username, int token, IPAddress ip, int port)
        {
            var mocks = new Mocks();
            mocks.Client.Setup(m => m.Options)
                .Returns(new ClientOptions(minimumDiagnosticLevel: DiagnosticLevel.Debug));

            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("F")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .Build();

            var diagnostics = new List<DiagnosticEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);

            handler.HandleMessage(null, msg);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Debug)
                .Where(d => d.Message.IndexOf("ignored", StringComparison.InvariantCultureIgnoreCase) > -1)
                .ToList();

            Assert.Single(diagnostics);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises DiagnosticGenerated on unknown ConnectToPeerResponse 'X'"), AutoData]
        public void Raises_DiagnosticGenerated_On_Ignored_ConnectToPeerResponse_X(string username, int token, IPAddress ip, int port)
        {
            var mocks = new Mocks();
            mocks.Client.Setup(m => m.Options)
                .Returns(new ClientOptions(minimumDiagnosticLevel: DiagnosticLevel.Debug));

            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("X")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .Build();

            var diagnostics = new List<DiagnosticEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);

            handler.HandleMessage(null, msg);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Debug)
                .Where(d => d.Message.IndexOf("unknown", StringComparison.InvariantCultureIgnoreCase) > -1)
                .ToList();

            Assert.Single(diagnostics);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Attempts connection on expected ConnectToPeerResponse 'F'"), AutoData]
        public void Attempts_Connection_On_Expected_ConnectToPeerResponse_F(string filename, string username, int token, IPAddress ip, int port)
        {
            var active = new ConcurrentDictionary<int, Transfer>();
            active.TryAdd(token, new Transfer(TransferDirection.Download, username, filename, token));

            var mocks = new Mocks();
            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            var transfer = new Transfer(TransferDirection.Download, username, "foo", token);
            transfer.RemoteToken = token;

            mocks.Downloads.TryAdd(token, transfer);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("F")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .Build();

            var conn = new Mock<IConnection>();
            conn.Setup(m => m.ReadAsync(4, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(new byte[] { 0, 0, 0, 0 }));

            mocks.PeerConnectionManager.Setup(m => m.GetTransferConnectionAsync(It.IsAny<ConnectToPeerResponse>()))
                .Returns(Task.FromResult((conn.Object, token)));

            handler.HandleMessage(null, msg);

            mocks.PeerConnectionManager.Verify(m => m.GetTransferConnectionAsync(It.IsAny<ConnectToPeerResponse>()), Times.Once);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Adds child connection on ConnectToPeerResponse 'D'"), AutoData]
        public void Adds_Child_Connection_On_ConnectToPeerResponse_D(string username, int token, IPAddress ip, int port)
        {
            ConnectToPeerResponse result = null;
            var (handler, mocks) = GetFixture();

            mocks.DistributedConnectionManager
                .Setup(m => m.AddChildConnectionAsync(It.IsAny<ConnectToPeerResponse>()))
                .Callback<ConnectToPeerResponse>(r => result = r);

            var ipBytes = ip.GetAddressBytes();
            Array.Reverse(ipBytes);

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .WriteString(username)
                .WriteString("D")
                .WriteBytes(ipBytes)
                .WriteInteger(port)
                .WriteInteger(token)
                .Build();

            handler.HandleMessage(null, msg);

            Assert.Equal(username, result.Username);
            Assert.Equal(token, result.Token);
            Assert.Equal(port, result.Port);
            Assert.Equal(ip, result.IPAddress);
        }

        [Trait("Category", "Message")]
        [Fact(DisplayName = "Raises DiagnosticGenerated on Exception")]
        public void Raises_DiagnosticGenerated_On_Exception()
        {
            var mocks = new Mocks();
            var handler = new ServerMessageHandler(
                mocks.Client.Object);

            mocks.Diagnostic.Setup(m => m.Debug(It.IsAny<string>()));

            var msg = new MessageBuilder()
                .WriteCode(MessageCode.Server.ConnectToPeer)
                .Build();

            var diagnostics = new List<DiagnosticEventArgs>();

            handler.DiagnosticGenerated += (_, e) => diagnostics.Add(e);
            handler.HandleMessage(null, msg);

            diagnostics = diagnostics
                .Where(d => d.Level == DiagnosticLevel.Warning)
                .Where(d => d.Message.IndexOf("Error handling server message", StringComparison.InvariantCultureIgnoreCase) > -1)
                .ToList();

            Assert.Single(diagnostics);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerAddUser"), AutoData]
        public void Handles_ServerAddUser(string username, bool exists, UserStatus status, int averageSpeed, int downloadCount, int fileCount, int directoryCount, string countryCode)
        {
            AddUserResponse result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<AddUserResponse>()))
                .Callback<WaitKey, AddUserResponse>((key, response) => result = response);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.AddUser)
                .WriteString(username)
                .WriteByte(1) // exists = true
                .WriteInteger((int)status)
                .WriteInteger(averageSpeed)
                .WriteLong(downloadCount)
                .WriteInteger(fileCount)
                .WriteInteger(directoryCount)
                .WriteString(countryCode)
                .Build();

            handler.HandleMessage(null, message);

            Assert.Equal(username, result.Username);
            Assert.Equal(exists, result.Exists);
            Assert.Equal(status, result.Status);
            Assert.Equal(averageSpeed, result.AverageSpeed);
            Assert.Equal(downloadCount, result.DownloadCount);
            Assert.Equal(fileCount, result.FileCount);
            Assert.Equal(directoryCount, result.DirectoryCount);
            Assert.Equal(countryCode, result.CountryCode);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles ServerGetStatus"), AutoData]
        public void Handles_ServerGetStatus(string username, UserStatus status, bool privileged)
        {
            GetStatusResponse result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<GetStatusResponse>()))
                .Callback<WaitKey, GetStatusResponse>((key, response) => result = response);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetStatus)
                .WriteString(username)
                .WriteInteger((int)status)
                .WriteByte((byte)(privileged ? 1 : 0))
                .Build();

            handler.HandleMessage(null, message);

            Assert.Equal(username, result.Username);
            Assert.Equal(status, result.Status);
            Assert.Equal(privileged, result.Privileged);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Handles NetInfo"), AutoData]
        public void Handles_NetInfo(List<(string Username, IPAddress IPAddress, int Port)> parents)
        {
            IEnumerable<(string Username, IPAddress IPAddress, int Port)> result = null;
            var (handler, mocks) = GetFixture();

            mocks.DistributedConnectionManager
                .Setup(m => m.AddParentConnectionAsync(It.IsAny<IEnumerable<(string Username, IPAddress IPAddress, int Port)>>()))
                .Callback<IEnumerable<(string Username, IPAddress IPAddress, int Port)>>(list => result = list);

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .WriteInteger(parents.Count);

            foreach (var parent in parents)
            {
                builder.WriteString(parent.Username);

                var ipBytes = parent.IPAddress.GetAddressBytes();
                Array.Reverse(ipBytes);

                builder.WriteBytes(ipBytes);
                builder.WriteInteger(parent.Port);
            }

            var message = builder.Build();

            handler.HandleMessage(null, message);

            Assert.Equal(parents, result);
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Produces diagnostic on NetInfo Exception"), AutoData]
        public void Produces_Diagnostic_On_NetInfo_Exception(List<(string Username, IPAddress IPAddress, int Port)> parents)
        {
            IEnumerable<(string Username, IPAddress IPAddress, int Port)> result = null;
            var (handler, mocks) = GetFixture();

            mocks.DistributedConnectionManager
                .Setup(m => m.AddParentConnectionAsync(It.IsAny<IEnumerable<(string Username, IPAddress IPAddress, int Port)>>()))
                .Throws(new Exception("foo"));

            var builder = new MessageBuilder()
                .WriteCode(MessageCode.Server.NetInfo)
                .WriteInteger(parents.Count);

            foreach (var parent in parents)
            {
                builder.WriteString(parent.Username);

                var ipBytes = parent.IPAddress.GetAddressBytes();
                Array.Reverse(ipBytes);

                builder.WriteBytes(ipBytes);
                builder.WriteInteger(parent.Port);
            }

            var message = builder.Build();

            handler.HandleMessage(null, message);

            mocks.Diagnostic.Verify(m => m.Debug("Error handling NetInfo message: foo"));
        }

        [Trait("Category", "Message")]
        [Theory(DisplayName = "Raises UserStatusChanged on ServerGetStatus"), AutoData]
        public void Raises_UserStatusChanged_On_ServerGetStatus(string username, UserStatus status, bool privileged)
        {
            GetStatusResponse result = null;
            var (handler, mocks) = GetFixture();

            mocks.Waiter.Setup(m => m.Complete(It.IsAny<WaitKey>(), It.IsAny<GetStatusResponse>()))
                .Callback<WaitKey, GetStatusResponse>((key, response) => result = response);

            var message = new MessageBuilder()
                .WriteCode(MessageCode.Server.GetStatus)
                .WriteString(username)
                .WriteInteger((int)status)
                .WriteByte((byte)(privileged ? 1 : 0))
                .Build();

            UserStatusChangedEventArgs eventArgs = null;

            handler.UserStatusChanged += (sender, args) => eventArgs = args;

            handler.HandleMessage(null, message);

            Assert.Equal(username, eventArgs.Username);
            Assert.Equal(status, eventArgs.Status);
            Assert.Equal(privileged, eventArgs.Privileged);
        }

        private (ServerMessageHandler Handler, Mocks Mocks) GetFixture(ClientOptions clientOptions = null)
        {
            var mocks = new Mocks(clientOptions);

            var handler = new ServerMessageHandler(
                mocks.Client.Object,
                mocks.Diagnostic.Object);

            return (handler, mocks);
        }

        private class Mocks
        {
            public Mocks(ClientOptions clientOptions = null)
            {
                Client = new Mock<SoulseekClient>(clientOptions)
                {
                    CallBase = true,
                };

                Client.Setup(m => m.ServerConnection).Returns(ServerConnection.Object);
                Client.Setup(m => m.PeerConnectionManager).Returns(PeerConnectionManager.Object);
                Client.Setup(m => m.DistributedConnectionManager).Returns(DistributedConnectionManager.Object);
                Client.Setup(m => m.Waiter).Returns(Waiter.Object);
                Client.Setup(m => m.Downloads).Returns(Downloads);
                Client.Setup(m => m.State).Returns(SoulseekClientStates.Connected | SoulseekClientStates.LoggedIn);
                Client.Setup(m => m.Options).Returns(clientOptions ?? new ClientOptions());
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IMessageConnection> ServerConnection { get; } = new Mock<IMessageConnection>();
            public Mock<IPeerConnectionManager> PeerConnectionManager { get; } = new Mock<IPeerConnectionManager>();
            public Mock<IDistributedConnectionManager> DistributedConnectionManager { get; } = new Mock<IDistributedConnectionManager>();
            public Mock<IWaiter> Waiter { get; } = new Mock<IWaiter>();
            public ConcurrentDictionary<int, Transfer> Downloads { get; } = new ConcurrentDictionary<int, Transfer>();
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
        }
    }
}
