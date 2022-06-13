// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Lamar;

namespace Shipwright.Dataflows.EventSinks.Internal;

public class EventSinkHandlerFactoryTests
{
    Mock<IServiceContext> container = new( MockBehavior.Strict );
    IEventSinkHandlerFactory instance() => new EventSinkHandlerFactory( container?.Object! );

    public class Constructor : EventSinkHandlerFactoryTests
    {
        [Fact]
        public void requires_container()
        {
            container = null!;
            Assert.Throws<ArgumentNullException>( nameof(container), instance );
        }
    }

    public class Create : EventSinkHandlerFactoryTests
    {
        EventSink eventSink = new FakeEventSink();
        Dataflow dataflow = new();
        CancellationToken cancellationToken;
        Task<IEventSinkHandler> method() => instance().Create( eventSink, dataflow, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_eventSink( bool canceled )
        {
            cancellationToken = new( canceled );
            eventSink = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(eventSink), method );
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
        public async Task returns_handler_created_by_located_factory( bool canceled )
        {
            cancellationToken = new( canceled );
            var expected = new Mock<IEventSinkHandler>( MockBehavior.Strict ).Object;
            var factory = new Mock<IEventSinkHandlerFactory<FakeEventSink>>( MockBehavior.Strict );
            container.Setup( _ => _.GetInstance( typeof(IEventSinkHandlerFactory<FakeEventSink>) ) ).Returns( factory.Object );
            factory.Setup( _ => _.Create( (FakeEventSink)eventSink, dataflow, cancellationToken ) ).ReturnsAsync( expected );

            var actual = await method();
            actual.Should().Be( expected );
        }
    }
}
