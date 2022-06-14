// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using Shipwright.Dataflows.Sources;

namespace Shipwright.Dataflows.EventSinks.Internal;

public abstract class EventSinkHandlerCancellationDecoratorTests
{
    Mock<IEventSinkHandler> inner = new( MockBehavior.Strict );
    IEventSinkHandler instance() => new EventSinkHandlerCancellationDecorator( inner?.Object! );

    public class Constructor : EventSinkHandlerCancellationDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public abstract class NotifyDataflowStarting : EventSinkHandlerCancellationDecoratorTests
    {
        CancellationToken cancellationToken;
        Task method() => instance().NotifyDataflowStarting( cancellationToken );

        public class WhenCanceled : NotifyDataflowStarting
        {
            public WhenCanceled() => cancellationToken = new( true );

            [Fact]
            public async Task throws_operationCanceled()
            {
                await Assert.ThrowsAsync<OperationCanceledException>( method );
            }
        }

        public class WhenNotCanceled : NotifyDataflowStarting
        {
            public WhenNotCanceled() => cancellationToken = new( false );

            [Fact]
            public async Task calls_inner_handler()
            {
                inner.Setup( _ => _.NotifyDataflowStarting( cancellationToken ) ).Returns( Task.CompletedTask );
                await method();

                inner.Verify( _ => _.NotifyDataflowStarting( cancellationToken ), Times.Once() );
            }
        }
    }

    public abstract class NotifyDataflowCompleted : EventSinkHandlerCancellationDecoratorTests
    {
        CancellationToken cancellationToken;
        Task method() => instance().NotifyDataflowCompleted( cancellationToken );

        public class WhenCanceled : NotifyDataflowCompleted
        {
            public WhenCanceled() => cancellationToken = new( true );

            [Fact]
            public async Task throws_operationCanceled()
            {
                await Assert.ThrowsAsync<OperationCanceledException>( method );
            }
        }

        public class WhenNotCanceled : NotifyDataflowCompleted
        {
            public WhenNotCanceled() => cancellationToken = new( false );

            [Fact]
            public async Task calls_inner_handler()
            {
                inner.Setup( _ => _.NotifyDataflowCompleted( cancellationToken ) ).Returns( Task.CompletedTask );
                await method();
                inner.Verify( _ => _.NotifyDataflowCompleted( cancellationToken ), Times.Once() );
            }
        }
    }

    public abstract class NotifyRecordCompleted : EventSinkHandlerCancellationDecoratorTests
    {
        Record record = new Fixture().WithDataflowCustomization().Create<Record>();
        CancellationToken cancellationToken;
        Task method() => instance().NotifyRecordCompleted( record, cancellationToken );

        [Fact]
        public async Task requires_dataflow()
        {
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        public class WhenCanceled : NotifyRecordCompleted
        {
            public WhenCanceled() => cancellationToken = new( true );

            [Fact]
            public async Task throws_operationCanceled()
            {
                await Assert.ThrowsAsync<OperationCanceledException>( method );
            }
        }

        public class WhenNotCanceled : NotifyRecordCompleted
        {
            public WhenNotCanceled() => cancellationToken = new( false );

            [Fact]
            public async Task calls_inner_handler()
            {
                inner.Setup( _ => _.NotifyRecordCompleted( record, cancellationToken ) ).Returns( Task.CompletedTask );
                await method();
                inner.Verify( _ => _.NotifyRecordCompleted( record, cancellationToken ), Times.Once() );
            }
        }
    }

    public abstract class NotifySourceCompleted : EventSinkHandlerCancellationDecoratorTests
    {
        Source source = new FakeSource();
        CancellationToken cancellationToken;
        Task method() => instance().NotifySourceCompleted( source, cancellationToken );

        [Fact]
        public async Task requires_dataflow()
        {
            source = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(source), method );
        }

        public class WhenCanceled : NotifySourceCompleted
        {
            public WhenCanceled() => cancellationToken = new( true );

            [Fact]
            public async Task throws_operationCanceled()
            {
                await Assert.ThrowsAsync<OperationCanceledException>( method );
            }
        }

        public class WhenNotCanceled : NotifySourceCompleted
        {
            public WhenNotCanceled() => cancellationToken = new( false );

            [Fact]
            public async Task calls_inner_handler()
            {
                inner.Setup( _ => _.NotifySourceCompleted( source, cancellationToken ) ).Returns( Task.CompletedTask );
                await method();
                inner.Verify( _ => _.NotifySourceCompleted( source, cancellationToken ), Times.Once() );
            }
        }
    }
}
