// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Lamar;

namespace Shipwright.Dataflows.Sources.Internal;

public class SourceReaderFactoryTests
{
    Mock<IServiceContext> container = new( MockBehavior.Strict );
    ISourceReaderFactory instance() => new SourceReaderFactory( container?.Object! );

    public class Constructor : SourceReaderFactoryTests
    {
        [Fact]
        public void requires_container()
        {
            container = null!;
            Assert.Throws<ArgumentNullException>( nameof(container), instance );
        }
    }

    public class Create : SourceReaderFactoryTests
    {
        Source source = new FakeSource();
        Dataflow dataflow = new();
        CancellationToken cancellationToken;
        Task<ISourceReader> method() => instance().Create( source, dataflow, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_source( bool canceled )
        {
            cancellationToken = new( canceled );
            source = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(source), method );
        }

        [Theory]
        [BooleanCases]
        public async Task requires_dataflow( bool canceled )
        {
            cancellationToken = new( canceled );
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_reader_from_located_factory( bool canceled )
        {
            cancellationToken = new( canceled );

            var factory = new Mock<ISourceReaderFactory<FakeSource>>( MockBehavior.Strict );
            var reader = new Mock<ISourceReader>( MockBehavior.Strict ).Object;
            container.Setup( _ => _.GetInstance( typeof(ISourceReaderFactory<FakeSource>) ) ).Returns( factory.Object );
            factory.Setup( _ => _.Create( (FakeSource)source, dataflow, cancellationToken ) ).ReturnsAsync( reader );

            var actual = await method();
            actual.Should().Be( reader );
        }
    }
}
