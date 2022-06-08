// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Lamar;

namespace Shipwright.Dataflows.Transformations.Internal;

public class TransformationHandlerFactoryTests
{
    Mock<IServiceContext> container = new( MockBehavior.Strict );
    ITransformationHandlerFactory instance() => new TransformationHandlerFactory( container?.Object! );

    public class Constructor : TransformationHandlerFactoryTests
    {
        [Fact]
        public void requires_container()
        {
            container = null!;
            Assert.Throws<ArgumentNullException>( nameof(container), instance );
        }
    }

    public class Create : TransformationHandlerFactoryTests
    {
        Transformation transformation = new FakeTransformation();
        Dataflow dataflow = new();
        CancellationToken cancellationToken;
        Task<ITransformationHandler> method() => instance().Create( transformation, dataflow, cancellationToken );

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
        public async Task requires_dataflow( bool canceled )
        {
            cancellationToken = new( canceled );
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_handler_from_located_factory( bool canceled )
        {
            cancellationToken = new( canceled );
            var expected = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
            var factory = new Mock<ITransformationHandlerFactory<FakeTransformation>>( MockBehavior.Strict );
            container.Setup( _ => _.GetInstance( typeof(ITransformationHandlerFactory<FakeTransformation>) ) ).Returns( factory.Object );
            factory.Setup( _ => _.Create( (FakeTransformation)transformation, dataflow, cancellationToken ) ).ReturnsAsync( expected );

            var actual = await method();
            actual.Should().Be( expected );
        }
    }
}
