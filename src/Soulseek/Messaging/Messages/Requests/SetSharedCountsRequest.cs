// <copyright file="SetSharedCountsRequest.cs" company="JP Dillingham">
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

namespace Soulseek.Messaging.Messages
{
    /// <summary>
    ///     Informs the server of the current number of shared directories and files.
    /// </summary>
    public class SetSharedCountsRequest
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SetSharedCountsRequest"/> class.
        /// </summary>
        /// <param name="directoryCount">The number of shared directories.</param>
        /// <param name="fileCount">The number of shared files.</param>
        public SetSharedCountsRequest(int directoryCount, int fileCount)
        {
            DirectoryCount = directoryCount;
            FileCount = fileCount;
        }

        /// <summary>
        ///     Gets the number of shared directories.
        /// </summary>
        public int DirectoryCount { get; }

        /// <summary>
        ///     Gets the number of shared files.
        /// </summary>
        public int FileCount { get; }

        /// <summary>
        ///     Implicitly converts an instance to a <see cref="Message"/> via <see cref="ToMessage()"/>.
        /// </summary>
        /// <param name="instance">The instance to convert.</param>
        public static implicit operator Message(SetSharedCountsRequest instance)
        {
            return instance.ToMessage();
        }

        /// <summary>
        ///     Constructs a <see cref="Message"/> from this request.
        /// </summary>
        /// <returns>The constructed message.</returns>
        public Message ToMessage()
        {
            return new MessageBuilder()
                .Code(MessageCode.ServerSharedFoldersAndFiles)
                .WriteInteger(DirectoryCount)
                .WriteInteger(FileCount)
                .Build();
        }
    }
}