// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.ConditionalTests;

public class FactoryTests
{
    Mock<ITransformationHandlerFactory> transformationHandlerFactory = new( MockBehavior.Strict );
    ITransformationHandlerFactory<Conditional> instance() => new Conditional.Factory( transformationHandlerFactory?.Object! );

    public class Constructor : FactoryTests
    {
        [Fact]
        public void requires_transformationHandlerFactory()
        {
            transformationHandlerFactory = null!;
            Assert.Throws<ArgumentNullException>( nameof(transformationHandlerFactory), instance );
        }
    }

    public class Create : FactoryTests
    {
        Conditional transformation = new Fixture().WithDataflowCustomization().Create<Conditional>();
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
        public async Task returns_handler_with_inner_as_aggregate( bool canceled )
        {
            cancellationToken = new( canceled );
            var inner = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;

            var captured = new List<Transformation>();
            transformationHandlerFactory.Setup( _ => _.Create( Capture.In( captured ), cancellationToken ) ).ReturnsAsync( inner );

            var actual = await method();
            var handler = actual.Should().BeOfType<Conditional.Handler>().Subject;
            handler._transformation.Should().BeSameAs( transformation );
            handler._inner.Should().BeSameAs( inner );

            var aggregate = captured.Should().ContainSingle().Subject.Should().BeOfType<AggregateTransformation>().Subject;
            aggregate.Transformations.Should().BeEquivalentTo( transformation.Transformations );
        }
    }
}
