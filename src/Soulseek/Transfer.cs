// <copyright file="Transfer.cs" company="JP Dillingham">
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

    /// <summary>
    ///     A single file transfer.
    /// </summary>
    /// <remarks>
    ///     This DTO wouldn't be necessary if Json.NET didn't serialize internal properties by default.
    /// </remarks>
    public class Transfer
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="Transfer"/> class.
        /// </summary>
        /// <param name="direction">The transfer direction.</param>
        /// <param name="username">The username of the peer to or from which the file is to be transferred.</param>
        /// <param name="filename">The filename of the file to be transferred.</param>
        /// <param name="token">The unique token for the transfer.</param>
        /// <param name="state">The state of the transfer.</param>
        /// <param name="size">The size of the file to be transferred, in bytes.</param>
        /// <param name="bytesTransferred">The total number of bytes transferred.</param>
        /// <param name="averageSpeed">The current average download speed.</param>
        /// <param name="startTime">
        ///     The time at which the transfer transitioned into the <see cref="TransferStates.InProgress"/> state.
        /// </param>
        /// <param name="endTime">
        ///     The time at which the transfer transitioned into the <see cref="TransferStates.Completed"/> state.
        /// </param>
        /// <param name="remoteToken">The remote unique token for the transfer.</param>
        /// <param name="ipAddress">The ip address of the remote transfer connection, if one has been established.</param>
        /// <param name="port">The port of the remote transfer connection, if one has been established.</param>
        /// <param name="options">The options for the transfer.</param>
        public Transfer(
            TransferDirection direction,
            string username,
            string filename,
            int token,
            TransferStates state,
            long size,
            long bytesTransferred = 0,
            double averageSpeed = 0,
            DateTime? startTime = null,
            DateTime? endTime = null,
            int? remoteToken = null,
            IPAddress ipAddress = null,
            int? port = null,
            TransferOptions options = null)
        {
            Direction = direction;
            Username = username;
            Filename = filename;
            Token = token;
            State = state;
            Size = size;
            BytesTransferred = bytesTransferred;
            AverageSpeed = averageSpeed;
            StartTime = startTime;
            EndTime = endTime;
            RemoteToken = remoteToken;
            IPAddress = ipAddress;
            Port = port;

            // create a new instance of options so we can strip out delegates. these don't serialize well and they shouldn't be
            // invoked by any code working with this DTO.
            Options = new TransferOptions(
                governor: null,
                stateChanged: null,
                progressUpdated: null,
                options?.DisposeInputStreamOnCompletion ?? false,
                options?.DisposeOutputStreamOnCompletion ?? false);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Transfer"/> class.
        /// </summary>
        /// <param name="transferInternal">The internal instance from which to copy data.</param>
        internal Transfer(TransferInternal transferInternal)
            : this(
                transferInternal.Direction,
                transferInternal.Username,
                transferInternal.Filename,
                transferInternal.Token,
                transferInternal.State,
                transferInternal.Size,
                transferInternal.BytesTransferred,
                transferInternal.AverageSpeed,
                transferInternal.StartTime,
                transferInternal.EndTime,
                transferInternal.RemoteToken,
                transferInternal.IPAddress,
                transferInternal.Port,
                transferInternal.Options)
        {
        }

        /// <summary>
        ///     Gets the current average download speed.
        /// </summary>
        public double AverageSpeed { get; }

        /// <summary>
        ///     Gets the number of remaining bytes to be transferred.
        /// </summary>
        public long BytesRemaining => Size - BytesTransferred;

        /// <summary>
        ///     Gets the total number of bytes transferred.
        /// </summary>
        public long BytesTransferred { get; }

        /// <summary>
        ///     Gets the transfer direction.
        /// </summary>
        public TransferDirection Direction { get; }

        /// <summary>
        ///     Gets the current duration of the transfer, if it has been started.
        /// </summary>
        public TimeSpan? ElapsedTime => StartTime == null ? default(TimeSpan) : (EndTime ?? DateTime.Now) - StartTime;

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
        ///     Gets the options for the transfer, with delegates excluded.
        /// </summary>
        public TransferOptions Options { get; }

        /// <summary>
        ///     Gets the current progress in percent.
        /// </summary>
        public double PercentComplete => Size == 0 ? 0 : (BytesTransferred / (double)Size) * 100;

        /// <summary>
        ///     Gets the port of the remote transfer connection, if one has been established.
        /// </summary>
        public int? Port { get; }

        /// <summary>
        ///     Gets the projected remaining duration of the transfer.
        /// </summary>
        public TimeSpan? RemainingTime => AverageSpeed == 0 ? default : TimeSpan.FromSeconds(BytesRemaining / AverageSpeed);

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
        ///     Gets the unique token for the transfer.
        /// </summary>
        public int Token { get; }

        /// <summary>
        ///     Gets the username of the peer to or from which the file is to be transferred.
        /// </summary>
        public string Username { get; }
    }
}