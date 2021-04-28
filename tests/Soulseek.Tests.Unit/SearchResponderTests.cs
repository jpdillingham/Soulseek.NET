// <copyright file="SearchResponderTests.cs" company="JP Dillingham">
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

namespace Soulseek.Tests.Unit
{
    using System;
    using AutoFixture.Xunit2;
    using Moq;
    using Soulseek.Diagnostics;
    using Xunit;

    public class SearchResponderTests
    {
        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Instantiates properly")]
        public void Instantiates_Properly()
        {
            SearchResponder r = null;

            var ex = Record.Exception(() => (r, _) = GetFixture());

            Assert.Null(ex);
            Assert.NotNull(r);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Throws if SoulseekClient is null")]
        public void Throws_If_SoulseekClient_Is_Null()
        {
            var ex = Record.Exception(() => new SearchResponder(null));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
            Assert.Equal("soulseekClient", ((ArgumentNullException)ex).ParamName);
        }

        [Trait("Category", "Instantiation")]
        [Fact(DisplayName = "Ensures Diagnostic given null")]
        public void Ensures_Diagnostic_Given_Null()
        {
            var (_, mocks) = GetFixture();

            SearchResponder r = default;
            var ex = Record.Exception(() => r = new SearchResponder(mocks.Client.Object, null));

            Assert.Null(ex);
            Assert.NotNull(r.GetProperty<IDiagnosticFactory>("Diagnostic"));
        }

        [Trait("Category", "TryDiscard")]
        [Theory(DisplayName = "TryDiscard removes token from cache"), AutoData]
        public void TryDiscard_Removes_Token_From_Cache(int responseToken, string username, int token, string query, SearchResponse searchResponse)
        {
            var cache = GetCacheMock();
            var (responder, mocks) = GetFixture(new SoulseekClientOptions(searchResponseCache: cache.Object));

            (string Username, int Token, string Query, SearchResponse SearchResponse) record = (username, token, query, searchResponse);

            cache.Setup(m => m.TryRemove(responseToken, out record))
                .Returns(true);

            var removed = responder.TryDiscard(responseToken);

            Assert.True(removed);

            cache.Verify(m => m.TryRemove(responseToken, out record), Times.Once);
        }

        private (SearchResponder SearchResponder, Mocks Mocks) GetFixture(SoulseekClientOptions options = null)
        {
            var mocks = new Mocks(options);

            var responder = new SearchResponder(
                mocks.Client.Object,
                mocks.Diagnostic.Object);

            return (responder, mocks);
        }

        private Mock<ISearchResponseCache> GetCacheMock() => new Mock<ISearchResponseCache>();

        private class Mocks
        {
            public Mocks(SoulseekClientOptions clientOptions = null)
            {
                Client = new Mock<SoulseekClient>(clientOptions)
                {
                    CallBase = true,
                };
            }

            public Mock<SoulseekClient> Client { get; }
            public Mock<IDiagnosticFactory> Diagnostic { get; } = new Mock<IDiagnosticFactory>();
        }
    }
}
