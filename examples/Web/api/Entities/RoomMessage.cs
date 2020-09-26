namespace WebAPI.Entities
{
    using System;

    /// <summary>
    ///     A message sent to a room.
    /// </summary>
    public class RoomMessage
    {
        /// <summary>
        ///     The timestamp of the message.
        /// </summary>
        DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        ///     The username of the user who sent the message.
        /// </summary>
        string Username { get; set; }

        /// <summary>
        ///     The message.
        /// </summary>
        string Message { get; set; }
    }
}
