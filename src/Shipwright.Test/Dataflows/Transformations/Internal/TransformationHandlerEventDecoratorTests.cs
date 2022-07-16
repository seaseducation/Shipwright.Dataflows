// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using Microsoft.Extensions.Logging;

namespace Shipwright.Dataflows.Transformations.Internal;

public abstract class TransformationHandlerEventDecoratorTests
{
    Mock<ITransformationHandler> inner = new( MockBehavior.Strict );
    ITransformationHandler instance() => new TransformationHandlerEventDecorator( inner?.Object! );

    public class Constructor : TransformationHandlerEventDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public class DisposeAsync : TransformationHandlerEventDecoratorTests
    {
        ValueTask method() => instance().DisposeAsync();

        [Fact]
        public async Task disposes_inner_handler()
        {
            inner.Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );
            await method();
            inner.Verify( _ => _.DisposeAsync(), Times.Once() );
        }
    }

    public abstract class Transform : TransformationHandlerEventDecoratorTests
    {
        Record record = new Fixture().WithDataflowCustomization().Create<Record>();
        CancellationToken cancellationToken;
        Task method() => instance().Transform( record, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_record( bool canceled )
        {
            cancellationToken = new( canceled );
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        public class WhenNoEvents : Transform
        {
            public WhenNoEvents()
            {
                record.Events.Clear();
            }

            [Theory]
            [BooleanCases]
            public async Task calls_inner_transformation( bool canceled )
            {
                cancellationToken = new( canceled );
                inner.Setup( _ => _.Transform( record, cancellationToken ) ).Returns( Task.CompletedTask );
                await method();

                inner.Verify( _ => _.Transform( record, cancellationToken ), Times.Once() );
            }
        }

        public class WhenEventsShouldNotStopProcessing : WhenNoEvents
        {
            readonly Fixture _fixture = new ();

            public WhenEventsShouldNotStopProcessing()
            {
                record.Events.Add( _fixture.Create<LogEvent>() with { StopProcessing = false } );
            }
        }

        public class WhenEventsShouldStopProcessing : Transform
        {
            readonly Fixture _fixture = new ();

            public WhenEventsShouldStopProcessing()
            {
                record.Events.Add( _fixture.Create<LogEvent>() with { StopProcessing = true } );
            }

            [Theory]
            [BooleanCases]
            public async Task does_not_invoke_inner_transformation( bool canceled )
            {
                cancellationToken = new( canceled );
                await method();
                inner.Verify( _ => _.Transform( It.IsAny<Record>(), It.IsAny<CancellationToken>() ), Times.Never() );
            }
        }
    }
}
