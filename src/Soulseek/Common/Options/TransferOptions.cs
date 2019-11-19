// <copyright file="TransferOptions.cs" company="JP Dillingham">
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

namespace Soulseek.Options
{
    using System;

    /// <summary>
    ///     Options for transfer operations.
    /// </summary>
    public sealed class TransferOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransferOptions"/> class.
        /// </summary>
        /// <param name="stateChanged">The Action to invoke when the transfer changes state.</param>
        /// <param name="progressUpdated">The Action to invoke when the transfer receives data.</param>
        public TransferOptions(
            Action<TransferStateChangedEventArgs> stateChanged = null,
            Action<TransferProgressUpdatedEventArgs> progressUpdated = null)
        {
            StateChanged = stateChanged;
            ProgressUpdated = progressUpdated;
        }

        /// <summary>
        ///     Gets the Action to invoke when the transfer receives data.
        /// </summary>
        public Action<TransferProgressUpdatedEventArgs> ProgressUpdated { get; }

        /// <summary>
        ///     Gets the Action to invoke when the transfer changes state.
        /// </summary>
        public Action<TransferStateChangedEventArgs> StateChanged { get; }
    }
}