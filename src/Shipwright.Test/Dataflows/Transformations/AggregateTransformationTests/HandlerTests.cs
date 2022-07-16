// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;

namespace Shipwright.Dataflows.Transformations.AggregateTransformationTests;

public class HandlerTests
{
    List<Mock<ITransformationHandler>> handlers = new() { new( MockBehavior.Strict ), new( MockBehavior.Strict ) };
    ITransformationHandler instance() => new AggregateTransformation.Handler( handlers?.Select( _ => _.Object ).ToArray()! );

    public class Constructor : HandlerTests
    {
        [Fact]
        public void requires_handlers()
        {
            handlers = null!;
            Assert.Throws<ArgumentNullException>( nameof(handlers), instance );
        }
    }

    public class DisposeAsync : HandlerTests
    {
        ValueTask method() => instance().DisposeAsync();

        [Fact]
        public async Task disposes_child_handlers()
        {
            foreach ( var handler in handlers )
                handler.Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask ).Verifiable();

            await method();

            foreach ( var handler in handlers )
                handler.Verify( _ => _.DisposeAsync(), Times.Once() );
        }
    }

    public class Transform : HandlerTests
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

        [Theory]
        [BooleanCases]
        public async Task invokes_child_handlers_in_sequence( bool canceled )
        {
            cancellationToken = new( canceled );

            var sequence = new MockSequence();

            foreach ( var handler in handlers )
                handler.InSequence( sequence ).Setup( _ => _.Transform( record, cancellationToken ) ).Returns( Task.CompletedTask ).Verifiable();

            await method();

            foreach ( var handler in handlers )
                handler.Verify( _ => _.Transform( record, cancellationToken ), Times.Once() );
        }
    }
}
