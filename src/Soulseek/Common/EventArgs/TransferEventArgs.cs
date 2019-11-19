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
        /// <param name="username">The username of the user to or from which the file is to be transferred.</param>
        /// <param name="filename">The filename of the file to be transferred.</param>
        /// <param name="size">The size of the file to be transferred, in bytes.</param>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="remoteToken">The remote unique token for the transfer.</param>
        /// <param name="direction">The transfer direction (upload or download).</param>
        /// <param name="state">The state of the transfer.</param>
        /// <param name="ipAddress">The IP address of the remote transfer connection, if one has been established.</param>
        /// <param name="port">The port of the remote transfer connection, if one has been established.</param>
        /// <param name="startTime">The time at which the transfer transitioned into the <see cref="TransferStates.InProgress"/> state.</param>
        /// <param name="endTime">The time at which the transfer transitioned into the <see cref="TransferStates.Completed"/> state.</param>
        /// <param name="bytesTransferred">The total number of bytes transferred.</param>
        /// <param name="bytesRemaining">The number of remaining bytes to be transferred.</param>
        /// <param name="percentComplete">The current progress in percent.</param>
        /// <param name="elapsedTime">The current duration of the transfer, if it has been started.</param>
        /// <param name="remainingTime">The projected remaining duration of the transfer.</param>
        /// <param name="averageSpeed">The current average transfer speed.</param>
        /// <param name="data">The data transferred.</param>
        /// <param name="options">The options for the transfer.</param>
        public TransferEventArgs(
            string username,
            string filename,
            long size,
            int token,
            int? remoteToken,
            TransferDirection direction,
            TransferStates state,
            IPAddress ipAddress,
            int? port,
            DateTime? startTime,
            DateTime? endTime,
            long bytesTransferred,
            long bytesRemaining,
            double percentComplete,
            TimeSpan? elapsedTime,
            TimeSpan? remainingTime,
            double averageSpeed,
            byte[] data,
            TransferOptions options)
        {
            Username = username;
            Filename = filename;
            Size = size;
            Token = token;
            RemoteToken = remoteToken;
            Direction = direction;
            State = state;

            IPAddress = ipAddress;
            Port = port;

            StartTime = startTime;
            EndTime = endTime;
            BytesTransferred = bytesTransferred;
            BytesRemaining = bytesRemaining;
            PercentComplete = percentComplete;
            ElapsedTime = elapsedTime;
            RemainingTime = remainingTime;
            AverageSpeed = averageSpeed;
            Data = data;
            Options = options;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferEventArgs"/> class.
        /// </summary>
        /// <param name="transfer">The transfer which raised the event.</param>
        internal TransferEventArgs(Transfer transfer)
            : this(
                  transfer.Username,
                  transfer.Filename,
                  transfer.Size,
                  transfer.Token,
                  transfer.RemoteToken,
                  transfer.Direction,
                  transfer.State,
                  transfer.IPAddress,
                  transfer.Connection?.Port,
                  transfer.StartTime,
                  transfer.EndTime,
                  transfer.BytesTransferred,
                  transfer.BytesRemaining,
                  transfer.PercentComplete,
                  transfer.ElapsedTime,
                  transfer.RemainingTime,
                  transfer.AverageSpeed,
                  transfer.Data,
                  transfer.Options)
        {
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

        /// <summary>
        ///     Gets the transfer direction (upload or download).
        /// </summary>
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
        /// <param name="username">The username of the user to or from which the file is to be transferred.</param>
        /// <param name="filename">The filename of the file to be transferred.</param>
        /// <param name="size">The size of the file to be transferred, in bytes.</param>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="remoteToken">The remote unique token for the transfer.</param>
        /// <param name="direction">The transfer direction (upload or download).</param>
        /// <param name="state">The state of the transfer.</param>
        /// <param name="ipAddress">The IP address of the remote transfer connection, if one has been established.</param>
        /// <param name="port">The port of the remote transfer connection, if one has been established.</param>
        /// <param name="startTime">The time at which the transfer transitioned into the <see cref="TransferStates.InProgress"/> state.</param>
        /// <param name="endTime">The time at which the transfer transitioned into the <see cref="TransferStates.Completed"/> state.</param>
        /// <param name="bytesTransferred">The total number of bytes transferred.</param>
        /// <param name="bytesRemaining">The number of remaining bytes to be transferred.</param>
        /// <param name="percentComplete">The current progress in percent.</param>
        /// <param name="elapsedTime">The current duration of the transfer, if it has been started.</param>
        /// <param name="remainingTime">The projected remaining duration of the transfer.</param>
        /// <param name="averageSpeed">The current average transfer speed.</param>
        /// <param name="data">The data transferred.</param>
        /// <param name="options">The options for the transfer.</param>
        public TransferProgressUpdatedEventArgs(
            long previousBytesTransferred,
            string username,
            string filename,
            long size,
            int token,
            int? remoteToken,
            TransferDirection direction,
            TransferStates state,
            IPAddress ipAddress,
            int? port,
            DateTime? startTime,
            DateTime? endTime,
            long bytesTransferred,
            long bytesRemaining,
            double percentComplete,
            TimeSpan? elapsedTime,
            TimeSpan? remainingTime,
            double averageSpeed,
            byte[] data,
            TransferOptions options)
            : base(
                  username,
                  filename,
                  size,
                  token,
                  remoteToken,
                  direction,
                  state,
                  ipAddress,
                  port,
                  startTime,
                  endTime,
                  bytesTransferred,
                  bytesRemaining,
                  percentComplete,
                  elapsedTime,
                  remainingTime,
                  averageSpeed,
                  data,
                  options)
        {
            PreviousBytesTransferred = previousBytesTransferred;
        }

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
        /// <param name="username">The username of the user to or from which the file is to be transferred.</param>
        /// <param name="filename">The filename of the file to be transferred.</param>
        /// <param name="size">The size of the file to be transferred, in bytes.</param>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="remoteToken">The remote unique token for the transfer.</param>
        /// <param name="direction">The transfer direction (upload or download).</param>
        /// <param name="state">The state of the transfer.</param>
        /// <param name="ipAddress">The IP address of the remote transfer connection, if one has been established.</param>
        /// <param name="port">The port of the remote transfer connection, if one has been established.</param>
        /// <param name="startTime">The time at which the transfer transitioned into the <see cref="TransferStates.InProgress"/> state.</param>
        /// <param name="endTime">The time at which the transfer transitioned into the <see cref="TransferStates.Completed"/> state.</param>
        /// <param name="bytesTransferred">The total number of bytes transferred.</param>
        /// <param name="bytesRemaining">The number of remaining bytes to be transferred.</param>
        /// <param name="percentComplete">The current progress in percent.</param>
        /// <param name="elapsedTime">The current duration of the transfer, if it has been started.</param>
        /// <param name="remainingTime">The projected remaining duration of the transfer.</param>
        /// <param name="averageSpeed">The current average transfer speed.</param>
        /// <param name="data">The data transferred.</param>
        /// <param name="options">The options for the transfer.</param>
        public TransferStateChangedEventArgs(
            TransferStates previousState,
            string username,
            string filename,
            long size,
            int token,
            int? remoteToken,
            TransferDirection direction,
            TransferStates state,
            IPAddress ipAddress,
            int? port,
            DateTime? startTime,
            DateTime? endTime,
            long bytesTransferred,
            long bytesRemaining,
            double percentComplete,
            TimeSpan? elapsedTime,
            TimeSpan? remainingTime,
            double averageSpeed,
            byte[] data,
            TransferOptions options)
            : base(
                  username,
                  filename,
                  size,
                  token,
                  remoteToken,
                  direction,
                  state,
                  ipAddress,
                  port,
                  startTime,
                  endTime,
                  bytesTransferred,
                  bytesRemaining,
                  percentComplete,
                  elapsedTime,
                  remainingTime,
                  averageSpeed,
                  data,
                  options)
        {
            PreviousState = previousState;
        }

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