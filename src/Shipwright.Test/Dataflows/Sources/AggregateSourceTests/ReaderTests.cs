// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;

namespace Shipwright.Dataflows.Sources.AggregateSourceTests;

public class ReaderTests
{
    IEnumerable<Mock<ISourceReader>> readers = new List<Mock<ISourceReader>> { new( MockBehavior.Strict ), new( MockBehavior.Strict ) };
    ISourceReader instance() => new AggregateSource.Reader( readers?.Select( _ => _.Object )! );

    public class Constructor : ReaderTests
    {
        [Fact]
        public void requires_readers()
        {
            readers = null!;
            Assert.Throws<ArgumentNullException>( nameof(readers), instance );
        }
    }

    public class Read : ReaderTests
    {
        ValueTask<List<Record>> method() => instance().Read( default ).ToListAsync();

        [Fact]
        public async Task returns_records_from_all_child_readers()
        {
            var fixture = new Fixture().WithDataflowCustomization();
            var expected = new List<Record>();

            foreach ( var reader in readers )
            {
                var records = fixture.CreateMany<Record>().ToArray();
                reader.Setup( _ => _.Read( default ) ).Returns( records.ToAsyncEnumerable() );
                expected.AddRange( records );
            }

            var actual = await method();
            actual.Should().ContainInOrder( expected );
        }
    }
}
