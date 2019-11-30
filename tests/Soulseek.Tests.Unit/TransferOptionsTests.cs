// <copyright file="TransferOptionsTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture.Xunit2;
    using Xunit;

    public class TransferOptionsTests
    {
        [Trait("Category", "Instantiation")]
        [Theory(DisplayName = "Instantiates with given data"), AutoData]
        public void Instantiates_With_Defaults(
            bool disposeInput,
            bool disposeOutput,
            Func<Transfer, CancellationToken, Task> governor,
            Action<TransferStateChangedEventArgs> stateChanged,
            Action<TransferProgressUpdatedEventArgs> progressUpdated)
        {
            var o = new TransferOptions(
                governor,
                stateChanged,
                progressUpdated,
                disposeInput,
                disposeOutput);

            Assert.Equal(disposeInput, o.DisposeInputStreamOnCompletion);
            Assert.Equal(disposeOutput, o.DisposeOutputStreamOnCompletion);
            Assert.Equal(governor, o.Governor);
            Assert.Equal(stateChanged, o.StateChanged);
            Assert.Equal(progressUpdated, o.ProgressUpdated);
        }
    }
}
