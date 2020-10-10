namespace WebAPI.Trackers
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using WebAPI.Entities;

    public interface IRoomTracker
    {
        /// <summary>
        ///     Tracked rooms.
        /// </summary>
        ConcurrentDictionary<string, IList<RoomMessage>> Rooms { get; }

        /// <summary>
        ///     Adds a room and appends the specified <paramref name="message"/>, or just appends the message if the room exists.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="message"></param>
        void AddOrUpdate(string roomName, RoomMessage message);

        /// <summary>
        ///     Returns the list of messages for the specified <paramref name="roomName"/>, if it is tracked.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        bool TryGet(string roomName, out IList<RoomMessage> messages);

        /// <summary>
        ///     Removes a tracked room.
        /// </summary>
        /// <param name="roomName"></param>
        void TryRemove(string roomName);
    }
}