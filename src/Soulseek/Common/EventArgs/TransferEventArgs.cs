// <copyright file="TransferEventArgs.cs" company="JP Dillingham">
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

    /// <summary>
    ///     Generic event arguments for transfer events.
    /// </summary>
    public class TransferEventArgs : SoulseekClientEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferEventArgs"/> class.
        /// </summary>
        /// <param name="transfer">The transfer which raised the event.</param>
        internal TransferEventArgs(Transfer transfer)
        {
            Transfer = transfer;
        }

        /// <summary>
        ///     Gets the instance which raised the event.
        /// </summary>
        public Transfer Transfer { get; }
    }

    /// <summary>
    ///     Event arguments for events raised by an update to transfer progress.
    /// </summary>
    public sealed class TransferProgressUpdatedEventArgs : TransferEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferProgressUpdatedEventArgs"/> class.
        /// </summary>
        /// <param name="previousBytesTransferred">The previous total number of bytes transferred.</param>
        /// <param name="transfer">The transfer which raised the event.</param>
        internal TransferProgressUpdatedEventArgs(long previousBytesTransferred, Transfer transfer)
            : base(transfer)
        {
            PreviousBytesTransferred = previousBytesTransferred;
        }

        /// <summary>
        ///     Gets the total number of bytes transferred prior to the event.
        /// </summary>
        public long PreviousBytesTransferred { get; }
    }

    /// <summary>
    ///     Event arguments for events raised by a change in transfer state.
    /// </summary>
    public sealed class TransferStateChangedEventArgs : TransferEventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferStateChangedEventArgs"/> class.
        /// </summary>
        /// <param name="previousState">The previous state of the transfer.</param>
        /// <param name="transfer">The transfer which raised the event.</param>
        internal TransferStateChangedEventArgs(TransferStates previousState, Transfer transfer)
            : base(transfer)
        {
            PreviousState = previousState;
        }

        /// <summary>
        ///     Gets the previous state of the transfer.
        /// </summary>
        public TransferStates PreviousState { get; }
    }
}