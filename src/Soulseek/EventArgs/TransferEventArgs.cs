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
    using System.Net;
    using Soulseek.Options;

    /// <summary>
    ///     Generic event arguments for transfer events.
    /// </summary>
    public class TransferEventArgs : EventArgs
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferEventArgs"/> class.
        /// </summary>
        /// <param name="transfer">The transfer which raised the event.</param>
        internal TransferEventArgs(Transfer transfer)
        {
            Direction = transfer.Direction;
            AverageSpeed = transfer.AverageSpeed;
            BytesTransferred = transfer.BytesTransferred;
            BytesRemaining = transfer.BytesRemaining;
            Data = transfer.Data;
            ElapsedTime = transfer.ElapsedTime;
            EndTime = transfer.EndTime;
            Filename = transfer.Filename;
            IPAddress = transfer.IPAddress;
            PercentComplete = transfer.PercentComplete;
            Port = transfer.Connection?.Port;
            RemainingTime = transfer.RemainingTime;
            RemoteToken = transfer.RemoteToken;
            Size = transfer.Size;
            StartTime = transfer.StartTime;
            State = transfer.State;
            Token = transfer.Token;
            Username = transfer.Username;
            Options = transfer.Options;
        }

        /// <summary>
        ///     Gets the current average transfer speed.
        /// </summary>
        public double AverageSpeed { get; }

        /// <summary>
        ///     Gets the number of remaining bytes to be transferred.
        /// </summary>
        public long BytesRemaining { get; }

        /// <summary>
        ///     Gets the total number of bytes transferred.
        /// </summary>
        public long BytesTransferred { get; }

        /// <summary>
        ///     Gets the data transferred.
        /// </summary>
        public byte[] Data { get; }

        public TransferDirection Direction { get; }

        /// <summary>
        ///     Gets the current duration of the transfer, if it has been started.
        /// </summary>
        public TimeSpan? ElapsedTime { get; }

        /// <summary>
        ///     Gets the time at which the transfer transitioned into the <see cref="TransferStates.Completed"/> state.
        /// </summary>
        public DateTime? EndTime { get; }

        /// <summary>
        ///     Gets the filename of the file to be transferred.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     Gets the ip address of the remote transfer connection, if one has been established.
        /// </summary>
        public IPAddress IPAddress { get; }

        /// <summary>
        ///     Gets the options for the transfer.
        /// </summary>
        public TransferOptions Options { get; }

        /// <summary>
        ///     Gets the current progress in percent.
        /// </summary>
        public double PercentComplete { get; }

        /// <summary>
        ///     Gets the port of the remote transfer connection, if one has been established.
        /// </summary>
        public int? Port { get; }

        /// <summary>
        ///     Gets the projected remaining duration of the transfer.
        /// </summary>
        public TimeSpan? RemainingTime { get; }

        /// <summary>
        ///     Gets the remote unique token for the transfer.
        /// </summary>
        public int? RemoteToken { get; }

        /// <summary>
        ///     Gets the size of the file to be transferred, in bytes.
        /// </summary>
        public long Size { get; }

        /// <summary>
        ///     Gets the time at which the transfer transitioned into the <see cref="TransferStates.InProgress"/> state.
        /// </summary>
        public DateTime? StartTime { get; }

        /// <summary>
        ///     Gets the state of the transfer.
        /// </summary>
        public TransferStates State { get; }

        /// <summary>
        ///     Gets the unique token for thr transfer.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the username of the peer to or from which the file is to be transferred.
        /// </summary>
        public string Username { get; }
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