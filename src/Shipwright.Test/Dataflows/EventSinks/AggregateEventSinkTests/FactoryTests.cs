// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.EventSinks.AggregateEventSinkTests;

public class FactoryTests
{
    Mock<IEventSinkHandlerFactory> factory = new( MockBehavior.Strict );
    IEventSinkHandlerFactory<AggregateEventSink> instance() => new AggregateEventSink.Factory( factory?.Object! );

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
        AggregateEventSink eventSink = new() { EventSinks = { new FakeEventSink(), new FakeEventSink() } };
        Dataflow dataflow = new();
        CancellationToken cancellationToken;
        Task<IEventSinkHandler> method() => instance().Create( eventSink, dataflow, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_transformation( bool canceled )
        {
            cancellationToken = new( canceled );
            eventSink = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(eventSink), method );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_handler_with_child_handlers( bool canceled )
        {
            cancellationToken = new( canceled );

            var expected = new List<IEventSinkHandler>();
            var sequence = new MockSequence();

            foreach ( var child in eventSink.EventSinks )
            {
                var handler = new Mock<IEventSinkHandler>( MockBehavior.Strict ).Object;
                expected.Add( handler );
                factory.InSequence( sequence ).Setup( _ => _.Create( child, dataflow, cancellationToken ) ).ReturnsAsync( handler );
            }

            var actual = await method();
            var decorator = actual.Should().BeOfType<AggregateEventSink.Handler>().Subject;
            decorator._handlers.Should().ContainInOrder( expected );
        }
    }
}
