namespace WebAPI.Trackers
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using WebAPI.Entities;

    /// <summary>
    ///     Tracks private message conversations.
    /// </summary>
    public interface IConversationTracker
    {
        /// <summary>
        ///     Tracked private message conversations.
        /// </summary>
        ConcurrentDictionary<string, IList<PrivateMessage>> Conversations { get; }

        /// <summary>
        ///     Adds a private message conversation and appends the specified <paramref name="message"/>, or just appends the message if the conversation exists.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="message"></param>
        void AddOrUpdate(string username, PrivateMessage message);

        /// <summary>
        ///     Removes a tracked private message conversation for the specified user.
        /// </summary>
        /// <param name="username"></param>
        void TryRemove(string username);
    }
}
