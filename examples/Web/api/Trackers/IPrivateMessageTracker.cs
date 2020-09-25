using Soulseek;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WebAPI.Trackers
{
    /// <summary>
    ///     Tracks private messages.
    /// </summary>
    public interface IPrivateMessageTracker
    {
        /// <summary>
        ///     Tracked private message conversations.
        /// </summary>
        ConcurrentDictionary<string, IList<PrivateMessageReceivedEventArgs>> PrivateMessages { get; }

        /// <summary>
        ///     Adds a private message conversation and appends the specified <paramref name="message"/>, or just appends the message if the conversation exists.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="message"></param>
        void AddOrUpdate(string username, PrivateMessageReceivedEventArgs message);

        /// <summary>
        ///     Removes a tracked private message conversation for the specified user.
        /// </summary>
        /// <param name="username"></param>
        void TryRemove(string username);
    }
}
