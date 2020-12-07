namespace WebAPI.Trackers
{
    using Soulseek;
    using System.Collections.Concurrent;
    using WebAPI.Entities;

    /// <summary>
    ///     Tracks rooms.
    /// </summary>
    public interface IRoomTracker
    {
        /// <summary>
        ///     Tracked rooms.
        /// </summary>
        ConcurrentDictionary<string, Room> Rooms { get; }

        /// <summary>
        ///     Available rooms.
        /// </summary>
        ConcurrentDictionary<string, RoomInfo> AvailableRooms { get; }

        /// <summary>
        ///     Adds a room and appends the specified <paramref name="message"/>, or just appends the message if the room exists.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="message"></param>
        void AddOrUpdateMessage(string roomName, RoomMessage message);

        /// <summary>
        ///     Adds the specified room to the tracker
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="room"></param>
        void TryAddRoom(string roomName, Room room);

        /// <summary>
        ///     Adds the specified <paramref name="userData"/> to the specified room.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="userData"></param>
        void TryAddUser(string roomName, UserData userData);

        /// <summary>
        ///     Returns the list of messages for the specified <paramref name="roomName"/>, if it is tracked.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="room"></param>
        /// <returns></returns>
        bool TryGetRoom(string roomName, out Room room);

        /// <summary>
        ///     Removes a tracked room.
        /// </summary>
        /// <param name="roomName"></param>
        void TryRemoveRoom(string roomName);

        /// <summary>
        ///     Removes the specified <paramref name="username"/> from the specified room.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="username"></param>
        void TryRemoveUser(string roomName, string username);

        /// <summary>
        ///     Adds the specified room to the list of available rooms.
        /// </summary>
        /// <param name="room"></param>
        void AddOrUpdateAvailableRoom(RoomInfo room);

        /// <summary>
        ///     Removes an available room.
        /// </summary>
        /// <param name="room"></param>
        void TryRemoveAvailableRoom(RoomInfo room);
    }
}