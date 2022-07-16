// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Sources.AggregateSourceTests;

public class FactoryTests
{
    Mock<ISourceReaderFactory> factory = new( MockBehavior.Strict );
    ISourceReaderFactory<AggregateSource> instance() => new AggregateSource.Factory( factory?.Object! );

    public class Constructor : FactoryTests
    {
        [Fact]
        public void requires_factory()
        {
            factory = null!;
            Assert.Throws<ArgumentNullException>( nameof(factory), instance );
        }
    }

    public class Create : FactoryTests
    {
        AggregateSource source = new AggregateSource { Sources = { new FakeSource(), new FakeSource() } };
        Dataflow dataflow = new();
        Task<ISourceReader> method() => instance().Create( source, dataflow, default );

        [Fact]
        public async Task requires_source()
        {
            source = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(source), method );
        }

        [Fact]
        public async Task requires_dataflow()
        {
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        [Fact]
        public async Task returns_reader_composed_of_child_readers()
        {
            var expected = new List<ISourceReader>();

            foreach ( var child in source.Sources )
            {
                var reader = new Mock<ISourceReader>( MockBehavior.Strict ).Object;
                factory.Setup( _ => _.Create( child, dataflow, default ) ).ReturnsAsync( reader );
                expected.Add( reader );
            }

            var actual = await method();
            var decorator = actual.Should().BeOfType<AggregateSource.Reader>().Subject;
            decorator._readers.Should().ContainInOrder( expected );
        }
    }
}
