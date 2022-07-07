// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.ConditionalTests;

public class HandlerTests
{
    Conditional transformation = new Fixture().WithDataflowCustomization().Create<Conditional>();
    Mock<ITransformationHandler> inner = new( MockBehavior.Strict );
    ITransformationHandler instance() => new Conditional.Handler( transformation, inner?.Object! );

    public class Constructor : HandlerTests
    {
        [Fact]
        public void requires_transformation()
        {
            transformation = null!;
            Assert.Throws<ArgumentNullException>( nameof(transformation), instance );
        }

        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public abstract class Transform : HandlerTests
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

        public class WhenConditionReturnsFalse : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task does_not_call_inner_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                transformation = transformation with { When = ( _, _ ) => Task.FromResult( false ) };
                await method();
                inner.Verify( _ => _.Transform( record, cancellationToken ), Times.Never() );
            }
        }

        public class WhenConditionReturnsTrue : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task calls_inner_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                transformation = transformation with { When = ( _, _ ) => Task.FromResult( true ) };

                inner.Setup( _ => _.Transform( record, cancellationToken ) ).Returns( Task.CompletedTask );
                await method();
                inner.Verify( _ => _.Transform( record, cancellationToken ), Times.Once() );
            }
        }
    }
}
