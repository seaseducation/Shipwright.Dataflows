// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;

namespace Shipwright.Dataflows.Sources.Internal;

public class SourceReaderCancellationDecoratorTests
{
    Mock<ISourceReader> inner = new( MockBehavior.Strict );
    ISourceReader instance() => new SourceReaderCancellationDecorator( inner?.Object! );

    public class Constructor : SourceReaderCancellationDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public abstract class Read : SourceReaderCancellationDecoratorTests
    {
        CancellationToken cancellationToken;
        ValueTask<List<Record>> method() => instance().Read( cancellationToken ).ToListAsync();

        readonly Fixture fixture = new Fixture().WithDataflowCustomization();
        public class WhenCanceled : Read
        {
            [Fact]
            public async Task returns_no_records()
            {
                cancellationToken = new( true );
                var records = fixture.CreateMany<Record>();
                inner.Setup( _ => _.Read( cancellationToken ) ).Returns( records.ToAsyncEnumerable() );

                var actual = await method();
                actual.Should().BeEmpty();
            }
        }

        public class WhenNotCanceled : Read
        {
            [Fact]
            public async Task returns_records_from_decorated_reader()
            {
                cancellationToken = new( false );
                var records = fixture.CreateMany<Record>().ToArray();
                inner.Setup( _ => _.Read( cancellationToken ) ).Returns( records.ToAsyncEnumerable() );

                var actual = await method();
                actual.Should().ContainInOrder( records );
            }
        }
    }
}
