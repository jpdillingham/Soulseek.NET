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

namespace Soulseek.NET.Tests.Unit.Messaging.Messages
{
    using System;
    using Soulseek.NET.Messaging;
    using Soulseek.NET.Messaging.Messages;
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

            Assert.Equal(MessageCode.ServerAcknowledgePrivateMessage, msg.Code);
            Assert.Equal(8, msg.Length);

            var reader = new MessageReader(msg);

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

            Assert.Equal(MessageCode.ServerGetPeerAddress, msg.Code);
            Assert.Equal(name.Length + 8, msg.Length);

            var reader = new MessageReader(msg);

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

            Assert.Equal(MessageCode.ServerLogin, msg.Code);
            Assert.Equal(name.Length + password.Length + a.Hash.Length + 24, msg.Length);

            var reader = new MessageReader(msg);

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
            PeerBrowseRequest a = null;

            var ex = Record.Exception(() => a = new PeerBrowseRequest());

            Assert.Null(ex);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "PeerBrowseRequest")]
        [Fact(DisplayName = "PeerBrowseRequest constructs the correct Message")]
        public void PeerBrowseRequest_Constructs_The_Correct_Message()
        {
            var msg = new PeerBrowseRequest().ToMessage();

            Assert.Equal(MessageCode.PeerBrowseRequest, msg.Code);
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

            Assert.Equal(0x1, (byte)msg.Code);
            Assert.Equal(1 + 4 + name.Length + "P".Length + 8, msg.Length);

            var reader = msg.ToPeerMessageReader();

            Assert.Equal(name, reader.ReadString());
            Assert.Equal("P", reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PeerSearchRequest")]
        [Fact(DisplayName = "PeerSearchRequest instantiates properly")]
        public void PeerSearchRequest_Instantiates_Properly()
        {
            var text = Guid.NewGuid().ToString();
            var ticket = new Random().Next();
            var a = new PeerSearchRequest(text, ticket);

            Assert.Equal(text, a.SearchText);
            Assert.Equal(ticket, a.Ticket);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "PeerSearchRequest")]
        [Fact(DisplayName = "PeerSearchRequest constructs the correct Message")]
        public void PeerSearchRequest_Constructs_The_Correct_Message()
        {
            var text = Guid.NewGuid().ToString();
            var ticket = new Random().Next();
            var a = new PeerSearchRequest(text, ticket);
            var msg = a.ToMessage();

            Assert.Equal(MessageCode.PeerSearchRequest, msg.Code);
            Assert.Equal(4 + 4 + 4 + text.Length, msg.Length);

            var reader = new MessageReader(msg);

            Assert.Equal(ticket, reader.ReadInteger());
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

            Assert.Equal(0x0, (byte)msg.Code);
            Assert.Equal(1 + 4, msg.Length);

            var reader = msg.ToPeerMessageReader();

            Assert.Equal(token, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SearchRequest")]
        [Fact(DisplayName = "SearchRequest instantiates properly")]
        public void SearchRequest_Instantiates_Properly()
        {
            var text = Guid.NewGuid().ToString();
            var ticket = new Random().Next();
            var a = new SearchRequest(text, ticket);

            Assert.Equal(text, a.SearchText);
            Assert.Equal(ticket, a.Ticket);
        }

        [Trait("Category", "ToMessage")]
        [Trait("Request", "SearchRequest")]
        [Fact(DisplayName = "SearchRequest constructs the correct Message")]
        public void SearchRequest_Constructs_The_Correct_Message()
        {
            var text = Guid.NewGuid().ToString();
            var ticket = new Random().Next();
            var a = new SearchRequest(text, ticket);
            var msg = a.ToMessage();

            Assert.Equal(MessageCode.ServerFileSearch, msg.Code);
            Assert.Equal(4 + 4 + 4 + text.Length, msg.Length);

            var reader = new MessageReader(msg);

            Assert.Equal(ticket, reader.ReadInteger());
            Assert.Equal(text, reader.ReadString());
        }
    }
}
