using System;
using System.Collections.Generic;
using System.Net;

namespace Soulseek.NET.Messaging.Responses
{
    public class RoomListResponse
    {
        public IEnumerable<Room> Rooms => RoomList;

        private int RoomCount { get; set; }
        private int UserCountCount { get; set; }
        private List<Room> RoomList { get; set; } = new List<Room>();

        public static RoomListResponse Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerRoomList)
            {
                throw new MessageException($"Message Code mismatch creating Room List response (expected: {(int)MessageCode.ServerRoomList}, received: {(int)reader.Code}");
            }

            var response = new RoomListResponse
            {
                RoomCount = reader.ReadInteger()
            };

            for (int i = 0; i < response.RoomCount; i++)
            {
                response.RoomList.Add(new Room() { Name = reader.ReadString() });
            }

            response.UserCountCount = reader.ReadInteger();

            for (int i = 0; i < response.UserCountCount; i++)
            {
                response.RoomList[i].UserCount = reader.ReadInteger();
            }

            return response;
        }
    }

    public class Room
    {
        public string Name { get; set; }
        public int UserCount { get; set; }
    }
}
