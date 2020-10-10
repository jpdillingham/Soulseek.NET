namespace WebAPI.Trackers
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using WebAPI.Entities;

    public class RoomTracker : IRoomTracker
    {
        public RoomTracker(int messageLimit = 100)
        {
            MessageLimit = messageLimit;
        }

        /// <summary>
        ///     Tracked rooms.
        /// </summary>
        public ConcurrentDictionary<string, IList<RoomMessage>> Rooms { get; } = new ConcurrentDictionary<string, IList<RoomMessage>>();

        private int MessageLimit { get; }

        /// <summary>
        ///     Adds a room and appends the specified <paramref name="message"/>, or just appends the message if the room exists.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="message"></param>
        public void AddOrUpdate(string roomName, RoomMessage message)
        {
            Rooms.AddOrUpdate(roomName, new List<RoomMessage>() { message }, (_, messageList) =>
            {
                if (messageList.Count >= MessageLimit)
                {
                    messageList = messageList.TakeLast(MessageLimit - 1).ToList();
                }

                messageList.Add(message);
                return messageList;
            });
        }

        /// <summary>
        ///     Removes a tracked room.
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="messages"></param>
        public bool TryGet(string roomName, out IList<RoomMessage> messages) => Rooms.TryGetValue(roomName, out messages);

        /// <summary>
        ///     Returns the list of messages for the specified <paramref name="roomName"/>, if it is tracked.
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        public void TryRemove(string roomName) => Rooms.TryRemove(roomName, out _);
    }
}