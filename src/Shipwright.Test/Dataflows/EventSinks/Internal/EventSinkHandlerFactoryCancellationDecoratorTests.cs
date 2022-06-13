// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.EventSinks.Internal;

public class EventSinkHandlerFactoryCancellationDecoratorTests
{
    Mock<IEventSinkHandlerFactory<FakeEventSink>> inner = new( MockBehavior.Strict );
    IEventSinkHandlerFactory<FakeEventSink> instance() => new EventSinkHandlerFactoryCancellationDecorator<FakeEventSink>( inner?.Object! );

    public class Constructor : EventSinkHandlerFactoryCancellationDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public abstract class Create : EventSinkHandlerFactoryCancellationDecoratorTests
    {
        FakeEventSink eventSink = new();
        Dataflow dataflow = new();
        CancellationToken cancellationToken;
        Task<IEventSinkHandler> method() => instance().Create( eventSink, dataflow, cancellationToken );

        [Fact]
        public async Task requires_eventSink()
        {
            eventSink = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(eventSink), method );
        }

        [Fact]
        public async Task requires_dataflow()
        {
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        public class WhenCanceled : Create
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

        public class WhenNotCanceled : Create
        {
            public WhenNotCanceled()
            {
                cancellationToken = new( false );
            }

            [Fact]
            public async Task returns_decorated_handler()
            {
                var expected = new Mock<IEventSinkHandler>( MockBehavior.Strict ).Object;
                inner.Setup( _ => _.Create( eventSink, dataflow, cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                var decorator = actual.Should().BeOfType<EventSinkHandlerCancellationDecorator>().Subject;
                decorator._inner.Should().Be( expected );
            }
        }
    }
}
