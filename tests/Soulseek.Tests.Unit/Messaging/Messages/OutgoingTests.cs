// <copyright file="OutgoingTests.cs" company="JP Dillingham">
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

    public class OutgoingTests
    {
        [Trait("Category", "Instantiation")]
        [Trait("Request", "AcknowledgePrivateMessage")]
        [Fact(DisplayName = "AcknowledgePrivateMessage instantiates properly")]
        public void AcknowledgePrivateMessage_Instantiates_Properly()
        {
            var num = new Random().Next();
            var a = new AcknowledgePrivateMessageCommand(num);

            Assert.Equal(num, a.Id);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "AcknowledgePrivateMessage")]
        [Fact(DisplayName = "AcknowledgePrivateMessage constructs the correct Message")]
        public void AcknowledgePrivateMessage_Constructs_The_Correct_Message()
        {
            var num = new Random().Next();
            var msg = new AcknowledgePrivateMessageCommand(num).ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.AcknowledgePrivateMessage, code);

            // length + code + token
            Assert.Equal(4 + 4 + 4, msg.Length);
            Assert.Equal(num, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "UserAddressRequest")]
        [Fact(DisplayName = "UserAddressRequest instantiates properly")]
        public void UserAddressRequest_Instantiates_Properly()
        {
            var name = Guid.NewGuid().ToString();
            var a = new UserAddressRequest(name);

            Assert.Equal(name, a.Username);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "UserAddressRequest")]
        [Fact(DisplayName = "UserAddressRequest constructs the correct Message")]
        public void UserAddressRequest_Constructs_The_Correct_Message()
        {
            var name = Guid.NewGuid().ToString();
            var msg = new UserAddressRequest(name).ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.GetPeerAddress, code);

            // length + code + name length + name string
            Assert.Equal(4 + 4 + 4 + name.Length, msg.Length);
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

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "LoginRequest")]
        [Fact(DisplayName = "LoginRequest constructs the correct Message")]
        public void LoginRequest_Constructs_The_Correct_Message()
        {
            var name = Guid.NewGuid().ToString();
            var password = Guid.NewGuid().ToString();
            var a = new LoginRequest(name, password);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.Login, code);
            Assert.Equal(name.Length + password.Length + a.Hash.Length + 28, msg.Length);
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

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "PeerBrowseRequest")]
        [Fact(DisplayName = "PeerBrowseRequest constructs the correct Message")]
        public void PeerBrowseRequest_Constructs_The_Correct_Message()
        {
            var msg = new BrowseRequest().ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.BrowseRequest, code);
            Assert.Equal(8, msg.Length);
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

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "PeerSearchRequest")]
        [Theory(DisplayName = "PeerSearchRequest constructs the correct Message"), AutoData]
        public void PeerSearchRequest_Constructs_The_Correct_Message(string text, int token)
        {
            var a = new PeerSearchRequest(text, token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.SearchRequest, code);
            Assert.Equal(4 + 4 + 4 + 4 + text.Length, msg.Length);

            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(text, reader.ReadString());
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

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SearchRequest")]
        [Theory(DisplayName = "SearchRequest constructs the correct Message"), AutoData]
        public void SearchRequest_Constructs_The_Correct_Message(string text, int token)
        {
            var a = new SearchRequest(text, token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.FileSearch, code);
            Assert.Equal(4 + 4 + 4 + 4 + text.Length, msg.Length);
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(text, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "PeerInfoRequest")]
        [Fact(DisplayName = "PeerInfoRequest constructs the correct Message")]
        public void PeerInfoRequest_Constructs_The_Correct_Message()
        {
            var a = new UserInfoRequest();
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Peer>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Peer.InfoRequest, code);
            Assert.Equal(8, msg.Length);
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "PlaceInQueueRequest")]
        [Theory(DisplayName = "PlaceInQueueRequest instantiates properly"), AutoData]
        public void PlaceInQueueRequest_Instantiates_Properly(string filename)
        {
            var a = new PlaceInQueueRequest(filename);

            Assert.Equal(filename, a.Filename);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "PlaceInQueueRequest")]
        [Theory(DisplayName = "PlaceInQueueRequest constructs the correct Message"), AutoData]
        public void PlaceInQueueRequest_Constructs_The_Correct_Message(string filename)
        {
            var a = new PlaceInQueueRequest(filename);
            var msg = a.ToByteArray();

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

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "AddUserRequest")]
        [Theory(DisplayName = "AddUserRequest constructs the correct message"), AutoData]
        public void AddUserRequest_Constructs_The_Correct_Message(string username)
        {
            var a = new AddUserRequest(username);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.AddUser, code);
            Assert.Equal(username, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "UserStatusRequest")]
        [Theory(DisplayName = "UserStatusRequest instantiates properly"), AutoData]
        public void UserStatusRequest_Instantiates_Properly(string username)
        {
            var a = new UserStatusRequest(username);

            Assert.Equal(username, a.Username);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "UserStatusRequest")]
        [Theory(DisplayName = "UserStatusRequest constructs the correct message"), AutoData]
        public void UserStatusRequest_Constructs_The_Correct_Message(string username)
        {
            var a = new UserStatusRequest(username);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.GetStatus, code);
            Assert.Equal(username, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SetListenPort")]
        [Theory(DisplayName = "SetListenPort instantiates properly"), AutoData]
        public void SetListenPort_Instantiates_Properly(int port)
        {
            var a = new SetListenPortCommand(port);

            Assert.Equal(port, a.Port);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SetListenPort")]
        [Theory(DisplayName = "SetListenPort constructs the correct message"), AutoData]
        public void SetListenPort_Constructs_The_Correct_Message(int port)
        {
            var a = new SetListenPortCommand(port);
            var msg = a.ToByteArray();

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

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "ConnectToPeerRequest")]
        [Theory(DisplayName = "ConnectToPeerRequest constructs the correct message"), AutoData]
        public void ConnectToPeerRequest_Constructs_The_Correct_Message(int token, string username, string type)
        {
            var a = new ConnectToPeerRequest(token, username, type);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.ConnectToPeer, code);
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(username, reader.ReadString());
            Assert.Equal(type, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SetSharedCounts")]
        [Theory(DisplayName = "SetSharedCounts instantiates properly"), AutoData]
        public void SetSharedCounts_Instantiates_Properly(int dirs, int files)
        {
            var a = new SetSharedCountsCommand(dirs, files);

            Assert.Equal(dirs, a.DirectoryCount);
            Assert.Equal(files, a.FileCount);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SetSharedCounts")]
        [Theory(DisplayName = "SetSharedCounts constructs the correct message"), AutoData]
        public void SetSharedCounts_Constructs_The_Correct_Message(int dirs, int files)
        {
            var a = new SetSharedCountsCommand(dirs, files);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SharedFoldersAndFiles, code);
            Assert.Equal(dirs, reader.ReadInteger());
            Assert.Equal(files, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "SetOnlineStatus")]
        [Theory(DisplayName = "SetOnlineStatus instantiates properly"), AutoData]
        public void SetOnlineStatus_Instantiates_Properly(UserStatus status)
        {
            var a = new SetOnlineStatusCommand(status);

            Assert.Equal(status, a.Status);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "SetOnlineStatus")]
        [Theory(DisplayName = "SetOnlineStatus constructs the correct message"), AutoData]
        public void SetOnlineStatus_Constructs_The_Correct_Message(UserStatus status)
        {
            var a = new SetOnlineStatusCommand(status);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SetOnlineStatus, code);
            Assert.Equal((int)status, reader.ReadInteger());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "RoomMessageCommand")]
        [Theory(DisplayName = "RoomMessageCommand instantiates properly"), AutoData]
        public void RoomMessageCommand_Instantiates_Properly(string room, string msg)
        {
            var a = new RoomMessageCommand(room, msg);

            Assert.Equal(room, a.RoomName);
            Assert.Equal(msg, a.Message);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "RoomMessageCommand")]
        [Theory(DisplayName = "RoomMessageCommand constructs the correct message"), AutoData]
        public void RoomMessageCommand_Constructs_The_Correct_Message(string room, string m)
        {
            var a = new RoomMessageCommand(room, m);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.SayInChatRoom, code);
            Assert.Equal(room, reader.ReadString());
            Assert.Equal(m, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "JoinRoomRequest")]
        [Theory(DisplayName = "JoinRoomRequest instantiates properly"), AutoData]
        public void JoinRoomRequest_Instantiates_Properly(string room)
        {
            var a = new JoinRoomRequest(room);

            Assert.Equal(room, a.RoomName);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "JoinRoomRequest")]
        [Theory(DisplayName = "JoinRoomRequest constructs the correct message"), AutoData]
        public void JoinRoomRequest_Constructs_The_Correct_Message(string room)
        {
            var a = new JoinRoomRequest(room);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.JoinRoom, code);
            Assert.Equal(room, reader.ReadString());
        }

        [Trait("Category", "Instantiation")]
        [Trait("Request", "LeaveRoomRequest")]
        [Theory(DisplayName = "LeaveRoomRequest instantiates properly"), AutoData]
        public void LeaveRoomRequest_Instantiates_Properly(string room)
        {
            var a = new LeaveRoomRequest(room);

            Assert.Equal(room, a.RoomName);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "LeaveRoomRequest")]
        [Theory(DisplayName = "LeaveRoomRequest constructs the correct message"), AutoData]
        public void LeaveRoomRequest_Constructs_The_Correct_Message(string room)
        {
            var a = new LeaveRoomRequest(room);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.LeaveRoom, code);
            Assert.Equal(room, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "RoomListRequest")]
        [Fact(DisplayName = "RoomListRequest constructs the correct message")]
        public void RoomListRequest_Constructs_The_Correct_Message()
        {
            var a = new RoomListRequest();
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.RoomList, code);
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "UserSearchRequest")]
        [Theory(DisplayName = "UserSearchRequest constructs the correct message"), AutoData]
        public void UserSearchRequest_Constructs_The_Correct_Message(string username, string searchText, int token)
        {
            var a = new UserSearchRequest(username, searchText, token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.UserSearch, code);
            Assert.Equal(username, reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(searchText, reader.ReadString());
        }

        [Trait("Category", "ToByteArray")]
        [Trait("Request", "RoomSearchRequest")]
        [Theory(DisplayName = "RoomSearchRequest constructs the correct message"), AutoData]
        public void RoomSearchRequest_Constructs_The_Correct_Message(string roomName, string searchText, int token)
        {
            var a = new RoomSearchRequest(roomName, searchText, token);
            var msg = a.ToByteArray();

            var reader = new MessageReader<MessageCode.Server>(msg);
            var code = reader.ReadCode();

            Assert.Equal(MessageCode.Server.RoomSearch, code);
            Assert.Equal(roomName, reader.ReadString());
            Assert.Equal(token, reader.ReadInteger());
            Assert.Equal(searchText, reader.ReadString());
        }
    }
}
