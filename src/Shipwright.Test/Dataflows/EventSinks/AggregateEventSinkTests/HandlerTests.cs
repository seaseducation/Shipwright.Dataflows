// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;

namespace Shipwright.Dataflows.EventSinks.AggregateEventSinkTests;

public class HandlerTests
{
    List<Mock<IEventSinkHandler>> handlers = new() { new( MockBehavior.Strict ), new( MockBehavior.Strict ) };
    IEventSinkHandler instance() => new AggregateEventSink.Handler( handlers?.Select( _ => _.Object ).ToArray()! );

    readonly Fixture _fixture = new Fixture().WithDataflowCustomization();

    public class Constructor : HandlerTests
    {
        [Fact]
        public void requires_handlers()
        {
            handlers = null!;
            Assert.Throws<ArgumentNullException>( nameof(handlers), instance );
        }
    }

    public class NotifyDataflowStarting : HandlerTests
    {
        CancellationToken cancellationToken;
        Task method() => instance().NotifyDataflowStarting( cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task calls_inner_handlers( bool canceled )
        {
            cancellationToken = new( canceled );
            var sequence = new MockSequence();

            foreach ( var handler in handlers )
                handler.InSequence( sequence ).Setup( _ => _.NotifyDataflowStarting( cancellationToken ) ).Returns( Task.CompletedTask );

            await method();

            foreach ( var handler in handlers )
                handler.Verify( _ => _.NotifyDataflowStarting( cancellationToken ), Times.Once() );
        }
    }

    public class NotifyDataflowCompleted : HandlerTests
    {
        CancellationToken cancellationToken;
        Task method() => instance().NotifyDataflowCompleted( cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task calls_inner_handlers( bool canceled )
        {
            cancellationToken = new( canceled );
            var sequence = new MockSequence();

            foreach ( var handler in handlers )
                handler.InSequence( sequence ).Setup( _ => _.NotifyDataflowCompleted( cancellationToken ) ).Returns( Task.CompletedTask );

            await method();

            foreach ( var handler in handlers )
                handler.Verify( _ => _.NotifyDataflowCompleted( cancellationToken ), Times.Once() );
        }
    }

    public class NotifyRecordCompleted : HandlerTests
    {
        Record record;
        CancellationToken cancellationToken;
        Task method() => instance().NotifyRecordCompleted( record, cancellationToken );

        public NotifyRecordCompleted() => record = _fixture.Create<Record>();

        [Theory]
        [BooleanCases]
        public async Task requires_dataflow( bool canceled )
        {
            cancellationToken = new( canceled );
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        [Theory]
        [BooleanCases]
        public async Task calls_inner_handlers( bool canceled )
        {
            cancellationToken = new( canceled );
            var sequence = new MockSequence();

            foreach ( var handler in handlers )
                handler.InSequence( sequence ).Setup( _ => _.NotifyRecordCompleted( record, cancellationToken ) ).Returns( Task.CompletedTask );

            await method();

            foreach ( var handler in handlers )
                handler.Verify( _ => _.NotifyRecordCompleted( record, cancellationToken ), Times.Once() );
        }
    }
}
