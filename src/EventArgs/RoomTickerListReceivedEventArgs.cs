// <copyright file="RoomTickerListReceivedEventArgs.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Event arguments for events raised upon the join of a user to a chat room.
    /// </summary>
    public class RoomTickerListReceivedEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerListReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="roomName">The name of the room to which the list applies.</param>
        /// <param name="tickerCount">The number of tickers.</param>
        /// <param name="tickers">The list of room tickers.</param>
        public RoomTickerListReceivedEventArgs(string roomName, int tickerCount, IEnumerable<RoomTicker> tickers)
        {
            RoomName = roomName;
            TickerCount = tickerCount;
            Tickers = tickers.ToList().AsReadOnly();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="RoomTickerListReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="notification">The notification which raised the event.</param>
        internal RoomTickerListReceivedEventArgs(RoomTickerListNotification notification)
            : this(notification.RoomName, notification.TickerCount, notification.Tickers)
        {
        }

        /// <summary>
        ///     Gets the name of the room to which the list applies.
        /// </summary>
        public string RoomName { get; }

        /// <summary>
        ///     Gets the number of tickers.
        /// </summary>
        public int TickerCount { get; }

        /// <summary>
        ///     Gets the list of room tickers.
        /// </summary>
        public IReadOnlyCollection<RoomTicker> Tickers { get; }
    }
}
