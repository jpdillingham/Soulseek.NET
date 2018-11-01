namespace Soulseek.NET.Messaging.Responses
{
    using System.Collections.Generic;

    public static class RoomList
    {
        public static IEnumerable<Room> Parse(Message message)
        {
            var reader = new MessageReader(message);

            if (reader.Code != MessageCode.ServerRoomList)
            {
                throw new MessageException($"Message Code mismatch creating Room List response (expected: {(int)MessageCode.ServerRoomList}, received: {(int)reader.Code}");
            }
            
            var roomCount = reader.ReadInteger();
            var list = new List<Room>();

            for (int i = 0; i < roomCount; i++)
            {
                list.Add(new Room() { Name = reader.ReadString() });
            }

            var userCountCount = reader.ReadInteger();

            for (int i = 0; i < userCountCount; i++)
            {
                list[i].UserCount = reader.ReadInteger();
            }

            return list.AsReadOnly();
        }
    }
}
