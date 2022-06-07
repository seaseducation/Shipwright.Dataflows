// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Sources.Internal;

public class SourceReaderFactoryCancellationDecoratorTests
{
    Mock<ISourceReaderFactory<FakeSource>> inner = new( MockBehavior.Strict );
    ISourceReaderFactory<FakeSource> instance() => new SourceReaderFactoryCancellationDecorator<FakeSource>( inner?.Object! );

    public class Constructor : SourceReaderFactoryCancellationDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public abstract class Create : SourceReaderFactoryCancellationDecoratorTests
    {
        FakeSource source = new();
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

        public class WhenCanceled : Create
        {
            [Fact]
            public async Task throws_operationCanceled()
            {
                cancellationToken = new( true );
                await Assert.ThrowsAsync<OperationCanceledException>( method );
            }
        }

        public class WhenNotCanceled : Create
        {
            [Fact]
            public async Task returns_decorated_reader_from_decorated_factory()
            {
                cancellationToken = new( false );

                var reader = new Mock<ISourceReader>( MockBehavior.Strict ).Object;
                inner.Setup( _ => _.Create( source, dataflow, cancellationToken ) ).ReturnsAsync( reader );

                var actual = await method();
                var decorator = actual.Should().BeOfType<SourceReaderCancellationDecorator>().Subject;
                decorator._inner.Should().Be( reader );
            }
        }
    }
}
