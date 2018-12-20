// <copyright file="ConnectionEventArgs.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.NET.Tcp
{
    using System;

    /// <summary>
    ///     EventArgs for <see cref="Connection"/> events.
    /// </summary>
    internal abstract class ConnectionEventArgs : EventArgs
    {
    }

    /// <summary>
    ///     EventArgs for <see cref="Connection"/> events raised by the exchange of data with a remote host.
    /// </summary>
    internal sealed class ConnectionDataEventArgs : ConnectionEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionDataEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data associated with the event.</param>
        /// <param name="currentLength">The length of the event data.</param>
        /// <param name="totalLength">The total expected length of the data transfer.</param>
        internal ConnectionDataEventArgs(byte[] data, int currentLength, int totalLength)
        {
            Data = data;
            CurrentLength = currentLength;
            TotalLength = totalLength;
        }

        /// <summary>
        ///     Gets the length of the event data.
        /// </summary>
        public int CurrentLength { get; }

        /// <summary>
        ///     Gets the data associated with the event.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        ///     Gets the progress of the data transfer as a percentage of current and total data length.
        /// </summary>
        public double PercentComplete => (CurrentLength / (double)TotalLength) * 100d;

        /// <summary>
        ///     Gets the total expected length of the data transfer.
        /// </summary>
        public int TotalLength { get; }
    }

    /// <summary>
    ///     EventArgs for <see cref="Connection"/> events raised by a change of connection state.
    /// </summary>
    internal sealed class ConnectionStateChangedEventArgs : ConnectionEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConnectionStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The state from which the connection changed.</param>
        /// <param name="currentState">The state to which the connection changed.</param>
        /// <param name="message">The optional message describing the nature of the change.</param>
        internal ConnectionStateChangedEventArgs(ConnectionState previousState, ConnectionState currentState, string message = null)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Message = message;
        }

        /// <summary>
        ///     Gets the state to which the connection changed.
        /// </summary>
        public ConnectionState CurrentState { get; }

        /// <summary>
        ///     Gets the optional message describing the nature of the change.
        /// </summary>
        public string Message { get; }

        /// <summary>
        ///     Gets the state from which the connection changed.
        /// </summary>
        public ConnectionState PreviousState { get; }
    }
}