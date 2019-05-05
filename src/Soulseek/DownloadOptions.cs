// <copyright file="DownloadOptions.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;

    /// <summary>
    ///     Options for the download operation.
    /// </summary>
    public sealed class DownloadOptions
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadOptions"/> class.
        /// </summary>
        /// <param name="stateChanged">The Action to invoke when the download changes state.</param>
        /// <param name="progressUpdated">The Action to invoke when the download receives data.</param>
        public DownloadOptions(
            Action<DownloadStateChangedEventArgs> stateChanged = null,
            Action<DownloadProgressUpdatedEventArgs> progressUpdated = null)
        {
            StateChanged = stateChanged;
            ProgressUpdated = progressUpdated;
        }

        /// <summary>
        ///     Gets the Action to invoke when the download receives data.
        /// </summary>
        public Action<DownloadProgressUpdatedEventArgs> ProgressUpdated { get; }

        /// <summary>
        ///     Gets the Action to invoke when the download changes state.
        /// </summary>
        public Action<DownloadStateChangedEventArgs> StateChanged { get; }
    }
}