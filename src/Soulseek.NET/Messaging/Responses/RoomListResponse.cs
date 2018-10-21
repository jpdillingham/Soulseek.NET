using System;
using System.Collections.Generic;
using System.Net;

namespace Soulseek.NET.Messaging.Responses
{
    [MessageResponse(MessageCode.ServerRoomList)]
    public class RoomListResponse : IMessageResponse<RoomListResponse>
    {
        public IEnumerable<Room> Rooms => RoomList;

        private int RoomCount { get; set; }
        private int UserCountCount { get; set; }
        private List<Room> RoomList { get; set; } = new List<Room>();

        public RoomListResponse Map(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerRoomList)
            {
                throw new MessageException($"Message Code mismatch creating Room List response (expected: {(int)MessageCode.ServerRoomList}, received: {(int)reader.Code}");
            }

            RoomCount = reader.ReadInteger();

            for (int i = 0; i < RoomCount; i++)
            {
                RoomList.Add(new Room() { Name = reader.ReadString() });
            }

            UserCountCount = reader.ReadInteger();

            for (int i = 0; i < UserCountCount; i++)
            {
                RoomList[i].UserCount = reader.ReadInteger();
            }

            return this;
        }
    }

    public class Room
    {
        public string Name { get; set; }
        public int UserCount { get; set; }
    }
}
