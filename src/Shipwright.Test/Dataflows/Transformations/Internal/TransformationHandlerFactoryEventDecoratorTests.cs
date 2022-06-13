// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.Internal;

public abstract class TransformationHandlerFactoryEventDecoratorTests
{
    Mock<ITransformationHandlerFactory<FakeTransformation>> inner = new( MockBehavior.Strict );
    ITransformationHandlerFactory<FakeTransformation> instance() => new TransformationHandlerFactoryEventDecorator<FakeTransformation>( inner?.Object! );

    public class Constructor : TransformationHandlerFactoryEventDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public class Create : TransformationHandlerFactoryEventDecoratorTests
    {
        FakeTransformation transformation = new();
        CancellationToken cancellationToken;
        Task<ITransformationHandler> method() => instance().Create( transformation, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_transformation( bool canceled )
        {
            cancellationToken = new( canceled );
            transformation = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(transformation), method );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_decorated_handler_from_inner_factory( bool canceled )
        {
            cancellationToken = new( canceled );
            var expected = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
            inner.Setup( _ => _.Create( transformation, cancellationToken ) ).ReturnsAsync( expected );

            var actual = await method();
            var decorator = actual.Should().BeOfType<TransformationHandlerEventDecorator>().Subject;
            decorator._inner.Should().Be( expected );
        }
    }
}
