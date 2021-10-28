// <copyright file="DistributedBranchLevelCommand.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Messaging.Messages
{
    /// <summary>
    ///     Informs distributed children of the current branch level.
    /// </summary>
    internal sealed class DistributedBranchLevelCommand : IIncomingMessage, IOutgoingMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedBranchLevelCommand"/> class.
        /// </summary>
        /// <param name="level">The current branch level.</param>
        public DistributedBranchLevelCommand(int level)
        {
            Level = level;
        }

        /// <summary>
        ///     Gets the current branch level.
        /// </summary>
        public int Level { get; }

        /// <summary>
        ///     Creates a new instance of <see cref="DistributedBranchLevelCommand"/> from the specified <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">The byte array from which to parse.</param>
        /// <returns>The created instance.</returns>
        public static DistributedBranchLevelCommand FromByteArray(byte[] bytes)
        {
            var reader = new MessageReader<MessageCode.Distributed>(bytes);
            var code = reader.ReadCode();

            if (code != MessageCode.Distributed.BranchLevel)
            {
                throw new MessageException($"Message Code mismatch creating {nameof(DistributedBranchLevelCommand)} (expected: {(int)MessageCode.Distributed.BranchLevel}, received: {(int)code})");
            }

            var level = reader.ReadInteger();

            return new DistributedBranchLevelCommand(level);
        }

        /// <summary>
        ///     Constructs a <see cref="byte"/> array from this message.
        /// </summary>
        /// <returns>The constructed byte array.</returns>
        public byte[] ToByteArray()
        {
            return new MessageBuilder()
                .WriteCode(MessageCode.Distributed.BranchLevel)
                .WriteInteger(Level)
                .Build();
        }
    }
}