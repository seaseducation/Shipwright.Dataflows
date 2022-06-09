// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.Internal;

public class TransformationHandlerCancellationDecoratorTests
{
    Mock<ITransformationHandler> inner = new( MockBehavior.Strict );
    ITransformationHandler instance() => new TransformationHandlerCancellationDecorator( inner?.Object! );

    public class Constructor : TransformationHandlerCancellationDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public class DisposeAsync : TransformationHandlerCancellationDecoratorTests
    {
        ValueTask method() => instance().DisposeAsync();

        [Fact]
        public async Task disposes_decorated_handler()
        {
            inner.Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );
            await method();

            inner.Verify( _ => _.DisposeAsync(), Times.Once() );
        }
    }

    public abstract class Transform : TransformationHandlerCancellationDecoratorTests
    {
        Record record = new Fixture().WithDataflowCustomization().Create<Record>();
        CancellationToken cancellationToken;
        Task method() => instance().Transform( record, cancellationToken );

        [Fact]
        public async Task requires_record()
        {
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        public class WhenCanceled : Transform
        {
            public WhenCanceled()
            {
                cancellationToken = new( true );
            }

            [Fact]
            public async Task throws_operationCanceled()
            {
                await Assert.ThrowsAsync<OperationCanceledException>( method );
            }
        }

        public class WhenNotCanceled : Transform
        {
            public WhenNotCanceled()
            {
                cancellationToken = new( false );
            }

            [Fact]
            public async Task invokes_decorated_handler()
            {
                var records = new List<Record>();
                inner.Setup( _ => _.Transform( Capture.In( records ), cancellationToken ) ).Returns( Task.CompletedTask );

                await method();
                records.Should().ContainSingle().Subject.Should().Be( record );
            }
        }
    }
}
