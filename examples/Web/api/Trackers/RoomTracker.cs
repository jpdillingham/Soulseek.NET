namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using WebAPI.Entities;

    /// <summary>
    ///     Tracks rooms.
    /// </summary>
    public class RoomTracker : IRoomTracker
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTracker"/> class.
        /// </summary>
        /// <param name="messageLimit"></param>
        public RoomTracker(int messageLimit = 25)
        {
            MessageLimit = messageLimit;
        }

        /// <summary>
        ///     Tracked rooms.
        /// </summary>
        public ConcurrentDictionary<string, Room> Rooms { get; } = new ConcurrentDictionary<string, Room>();

        /// <summary>
        ///     Available rooms.
        /// </summary>
        public ConcurrentDictionary<string, RoomInfo> AvailableRooms { get; } = new ConcurrentDictionary<string, RoomInfo>();

        private int MessageLimit { get; }

        /// <summary>
        ///     Adds a room and appends the specified <paramref name="message"/>, or just appends the message if the room exists.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="message"></param>
        public void AddOrUpdateMessage(string roomName, RoomMessage message)
        {
            Rooms.AddOrUpdate(roomName, new Room() { Messages = new List<RoomMessage>() { message } }, (_, room) =>
            {
                if (room.Messages.Count >= MessageLimit)
                {
                    room.Messages = room.Messages.TakeLast(MessageLimit - 1).ToList();
                }

                room.Messages.Add(message);
                return room;
            });
        }

        /// <summary>
        ///     Adds the specified room to the tracker
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="room"></param>
        public void TryAddRoom(string roomName, Room room) => Rooms.TryAdd(roomName, room);

        /// <summary>
        ///     Adds the specified <paramref name="userData"/> to the specified room.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="userData"></param>
        public void TryAddUser(string roomName, UserData userData)
        {
            if (Rooms.TryGetValue(roomName, out var room))
            {
                room.Users.Add(userData);
            }
        }

        /// <summary>
        ///     Removes a tracked room.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="room"></param>
        public bool TryGetRoom(string roomName, out Room room) => Rooms.TryGetValue(roomName, out room);

        /// <summary>
        ///     Returns the list of messages for the specified <paramref name="roomName"/>, if it is tracked.
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        public void TryRemoveRoom(string roomName) => Rooms.TryRemove(roomName, out _);

        /// <summary>
        ///     Removes the specified <paramref name="username"/> from the specified room.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="username"></param>
        public void TryRemoveUser(string roomName, string username)
        {
            if (Rooms.TryGetValue(roomName, out var room))
            {
                room.Users = room.Users.Where(u => u.Username != username).ToList();
            }
        }

        /// <summary>
        ///     Adds the specified room to the list of available rooms.
        /// </summary>
        /// <param name="room"></param>
        public void AddOrUpdateAvailableRoom(RoomInfo room) => AvailableRooms.AddOrUpdate(room.Name, room, (_, existingRoom) => room);

        /// <summary>
        ///     Removes an available room.
        /// </summary>
        /// <param name="room"></param>
        public void TryRemoveAvailableRoom(RoomInfo room) => AvailableRooms.TryRemove(room.Name, out _);
    }
}