// <copyright file="RequestsTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit.Messaging.Messages
{
    using System;
    using AutoFixture.Xunit2;
    using Soulseek.Messaging;
    using Soulseek.Messaging.Messages;
    using Xunit;

    public class RequestsTests
    {
        [Trait("Category", "Instantiation")]
        [Trait("Request", "AcknowledgePrivateMessageRequest")]
        [Fact(DisplayName = "AcknowledgePrivateMessageRequest instantiates properly")]
        public void AcknowledgePrivateMessageRequest_Instantiates_Properly()
        {
            var num = new Random().Next();
            var a = new AcknowledgePrivateMessageRequest(num);

            Assert.Equal(num, a.Id);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "AcknowledgePrivateMessageRequest")]
        [Fact(DisplayName = "AcknowledgePrivateMessageRequest constructs the correct Message")]
        public void AcknowledgePrivateMessageRequest_Constructs_The_Correct_Message()
        {
            var num = new Random().Next();
            var msg = new AcknowledgePrivateMessageRequest(num).ToMessage();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.AcknowledgePrivateMessage, code);
            Assert.Equal(8, msg.Length);
            Assert.Equal(num, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "GetPeerAddressRequest")]
        [Fact(DisplayName = "GetPeerAddressRequest instantiates properly")]
        public void GetPeerAddressRequest_Instantiates_Properly()
        {
            var name = Guid.NewGuid().ToString();
            var a = new GetPeerAddressRequest(name);

            Assert.Equal(name, a.Username);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "GetPeerAddressRequest")]
        [Fact(DisplayName = "GetPeerAddressRequest constructs the correct Message")]
        public void GetPeerAddressRequest_Constructs_The_Correct_Message()
        {
            var name = Guid.NewGuid().ToString();
            var msg = new GetPeerAddressRequest(name).ToMessage();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.GetPeerAddress, code);
            Assert.Equal(name.Length + 8, msg.Length); 
            Assert.Equal(name, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "LoginRequest")]
        [Fact(DisplayName = "LoginRequest instantiates properly")]
        public void LoginRequest_Instantiates_Properly()
        {
            var name = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();
            var a = new LoginRequest(name, password);

            Assert.Equal(name, a.Username);
            Assert.Equal(password, a.Password);
            Assert.NotEmpty(a.Hash);
            Assert.NotEqual(0, a.Version);
            Assert.NotEqual(0, a.MinorVersion);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "LoginRequest")]
        [Fact(DisplayName = "LoginRequest constructs the correct Message")]
        public void LoginRequest_Constructs_The_Correct_Message()
        {
            var name = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();
            var a = new LoginRequest(name, password);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.Login, code);
            Assert.Equal(name.Length + password.Length + a.Hash.Length + 24, msg.Length);
            Assert.Equal(name, reader.ReadString());
            Assert.Equal(password, reader.ReadString());
            Assert.Equal(a.Version, reader.ReadInteger());
            Assert.Equal(a.Hash, reader.ReadString());
            Assert.Equal(a.MinorVersion, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PeerBrowseRequest")]
        [Fact(DisplayName = "PeerBrowseRequest instantiates properly")]
        public void PeerBrowseRequest_Instantiates_Properly()
        {
            BrowseRequest a = null;

            var ex = Record.Exception(() => a = new BrowseRequest());

            Assert.Null(ex);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "PeerBrowseRequest")]
        [Fact(DisplayName = "PeerBrowseRequest constructs the correct Message")]
        public void PeerBrowseRequest_Constructs_The_Correct_Message()
        {
            var msg = new BrowseRequest().ToMessage();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.BrowseRequest, code);
            Assert.Equal(4, msg.Length);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PeerInitRequest")]
        [Fact(DisplayName = "PeerInitRequest instantiates properly")]
        public void PeerInitRequest_Instantiates_Properly()
        {
            var name = Guid.NewGuid().ToString();
            var token = new Random().Next();
            var a = new PeerInitRequest(name, "P", token);

            Assert.Equal(name, a.Username);
            Assert.Equal("P", a.TransferType);
            Assert.Equal(token, a.Token);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "PeerInitRequest")]
        [Fact(DisplayName = "PeerInitRequest constructs the correct Message")]
        public void PeerInitRequest_Constructs_The_Correct_Message()
        {
            var name = Guid.NewGuid().ToString();
            var token = new Random().Next();
            var a = new PeerInitRequest(name, "P", token);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Initialization>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Initialization.PeerInit, code);
            Assert.Equal(1 + 4 + name.Length + "P".Length + 8, msg.Length);


            Assert.Equal(name, reader.ReadString());
            Assert.Equal("P", reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PeerSearchRequest")]
        [Theory(DisplayName = "PeerSearchRequest instantiates properly"), AutoData]
        public void PeerSearchRequest_Instantiates_Properly(string text, int token)
        {
            var a = new PeerSearchRequest(text, token);

            Assert.Equal(text, a.SearchText);
            Assert.Equal(token, a.Token);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "PeerSearchRequest")]
        [Theory(DisplayName = "PeerSearchRequest constructs the correct Message"), AutoData]
        public void PeerSearchRequest_Constructs_The_Correct_Message(string text, int token)
        {
            var a = new PeerSearchRequest(text, token);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.SearchRequest, code);
            Assert.Equal(4 + 4 + 4 + text.Length, msg.Length);

            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(text, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PierceFirewallRequest")]
        [Fact(DisplayName = "PierceFirewallRequest instantiates properly")]
        public void PierceFirewallRequest_Instantiates_Properly()
        {
            var token = new Random().Next();
            var a = new PierceFirewallRequest(token);

            Assert.Equal(token, a.Token);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "PierceFirewallRequest")]
        [Fact(DisplayName = "PierceFirewallRequest constructs the correct Message")]
        public void PierceFirewallRequest_Constructs_The_Correct_Message()
        {
            var token = new Random().Next();
            var a = new PierceFirewallRequest(token);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Initialization>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Initialization.PierceFirewall, code);
            Assert.Equal(1 + 4, msg.Length);

            Assert.Equal(token, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SearchRequest")]
        [Theory(DisplayName = "SearchRequest instantiates properly"), AutoData]
        public void SearchRequest_Instantiates_Properly(string text, int token)
        {
            var a = new SearchRequest(text, token);

            Assert.Equal(text, a.SearchText);
            Assert.Equal(token, a.Token);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "SearchRequest")]
        [Theory(DisplayName = "SearchRequest constructs the correct Message"), AutoData]
        public void SearchRequest_Constructs_The_Correct_Message(string text, int token)
        {
            var a = new SearchRequest(text, token);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.FileSearch, code);
            Assert.Equal(4 + 4 + 4 + text.Length, msg.Length);
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(text, reader.ReadString());
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "PeerInfoRequest")]
        [Fact(DisplayName = "PeerInfoRequest constructs the correct Message")]
        public void PeerInfoRequest_Constructs_The_Correct_Message()
        {
            var a = new UserInfoRequest();
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.InfoRequest, code);
            Assert.Equal(4, msg.Length);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PeerPlaceInQueueRequest")]
        [Theory(DisplayName = "PeerPlaceInQueueRequest instantiates properly"), AutoData]
        public void PeerPlaceInQueueRequest_Instantiates_Properly(string filename)
        {
            var a = new PeerPlaceInQueueRequest(filename);

            Assert.Equal(filename, a.Filename);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "PeerPlaceInQueueRequest")]
        [Theory(DisplayName = "PeerPlaceInQueueRequest constructs the correct Message"), AutoData]
        public void PeerPlaceInQueueRequest_Constructs_The_Correct_Message(string filename)
        {
            var a = new PeerPlaceInQueueRequest(filename);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.PlaceInQueueRequest, code);
            Assert.Equal(filename, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "AddUserRequest")]
        [Theory(DisplayName = "AddUserRequest instantiates properly"), AutoData]
        public void AddUserRequest_Instantiates_Properly(string username)
        {
            var a = new AddUserRequest(username);

            Assert.Equal(username, a.Username);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "AddUserRequest")]
        [Theory(DisplayName = "AddUserRequest constructs the correct message"), AutoData]
        public void AddUserRequest_Constructs_The_Correct_Message(string username)
        {
            var a = new AddUserRequest(username);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.AddUser, code);
            Assert.Equal(username, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "GetStatusRequest")]
        [Theory(DisplayName = "GetStatusRequest instantiates properly"), AutoData]
        public void GetStatusRequest_Instantiates_Properly(string username)
        {
            var a = new GetStatusRequest(username);

            Assert.Equal(username, a.Username);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "GetStatusRequest")]
        [Theory(DisplayName = "GetStatusRequest constructs the correct message"), AutoData]
        public void GetStatusRequest_Constructs_The_Correct_Message(string username)
        {
            var a = new GetStatusRequest(username);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.GetStatus, code);
            Assert.Equal(username, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SetListenPortRequest")]
        [Theory(DisplayName = "SetListenPortRequest instantiates properly"), AutoData]
        public void SetListenPortRequest_Instantiates_Properly(int port)
        {
            var a = new SetListenPortRequest(port);

            Assert.Equal(port, a.Port);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "SetListenPortRequest")]
        [Theory(DisplayName = "SetListenPortRequest constructs the correct message"), AutoData]
        public void SetListenPortRequest_Constructs_The_Correct_Message(int port)
        {
            var a = new SetListenPortRequest(port);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SetListenPort, code);
            Assert.Equal(port, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "ConnectToPeerRequest")]
        [Theory(DisplayName = "ConnectToPeerRequest instantiates properly"), AutoData]
        public void ConnectToPeerRequest_Instantiates_Properly(int token, string username, string type)
        {
            var a = new ConnectToPeerRequest(token, username, type);

            Assert.Equal(token, a.Token);
            Assert.Equal(username, a.Username);
            Assert.Equal(type, a.Type);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "ConnectToPeerRequest")]
        [Theory(DisplayName = "ConnectToPeerRequest constructs the correct message"), AutoData]
        public void ConnectToPeerRequest_Constructs_The_Correct_Message(int token, string username, string type)
        {
            var a = new ConnectToPeerRequest(token, username, type);
            var msg = a.ToMessage();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.ConnectToPeer, code);
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(username, reader.ReadString());
            Assert.Equal(type, reader.ReadString());
        }
    }
}
