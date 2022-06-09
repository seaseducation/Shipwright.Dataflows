// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.Internal;

public class TransformationHandlerFactoryCancellationDecoratorTests
{
    Mock<ITransformationHandlerFactory<FakeTransformation>> inner = new( MockBehavior.Strict );
    ITransformationHandlerFactory<FakeTransformation> instance() => new TransformationHandlerFactoryCancellationDecorator<FakeTransformation>( inner?.Object! );

    public class Constructor : TransformationHandlerFactoryCancellationDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public abstract class Create : TransformationHandlerFactoryCancellationDecoratorTests
    {
        FakeTransformation transformation = new();
        CancellationToken cancellationToken;
        Task<ITransformationHandler> method() => instance().Create( transformation, cancellationToken );

        [Fact]
        public async Task requires_transformation()
        {
            transformation = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(transformation), method );
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
                var expected = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
                inner.Setup( _ => _.Create( transformation, cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                var decorator = actual.Should().BeOfType<TransformationHandlerCancellationDecorator>().Subject;
                decorator._inner.Should().Be( expected );
            }
        }
    }
}
