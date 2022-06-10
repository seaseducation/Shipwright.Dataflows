// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.AggregateTransformationTests;

public class FactoryTests
{
    Mock<ITransformationHandlerFactory> factory = new( MockBehavior.Strict );
    ITransformationHandlerFactory<AggregateTransformation> instance() => new AggregateTransformation.Factory( factory?.Object! );

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
        AggregateTransformation transformation = new() { Transformations = { new FakeTransformation(), new FakeTransformation() } };
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
        public async Task rethrows_exceptions_and_disposes_child_handlers( bool canceled )
        {
            cancellationToken = new( canceled );

            var handlers = new List<Mock<ITransformationHandler>>();
            var sequence = new MockSequence();

            foreach ( var child in transformation.Transformations )
            {
                var handler = new Mock<ITransformationHandler>( MockBehavior.Strict );
                handlers.Add( handler );
                factory.InSequence( sequence ).Setup( _ => _.Create( child, cancellationToken ) ).ReturnsAsync( handler.Object );
                handler.Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask ).Verifiable();
            }

            var exceptional = new FakeTransformation();
            transformation.Transformations.Add( exceptional );
            var expected = new Exception();
            factory.InSequence( sequence ).Setup( _ => _.Create( exceptional, cancellationToken ) ).ThrowsAsync( expected );

            var actual = await Assert.ThrowsAsync<Exception>( method );
            actual.Should().BeSameAs( expected );

            foreach ( var handler in handlers )
                handler.Verify( _ => _.DisposeAsync(), Times.Once() );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_handler_with_child_handlers( bool canceled )
        {
            cancellationToken = new( canceled );

            var expected = new List<ITransformationHandler>();
            var sequence = new MockSequence();

            foreach ( var child in transformation.Transformations )
            {
                var handler = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
                expected.Add( handler );
                factory.InSequence( sequence ).Setup( _ => _.Create( child, cancellationToken ) ).ReturnsAsync( handler );
            }

            var actual = await method();
            var decorator = actual.Should().BeOfType<AggregateTransformation.Handler>().Subject;
            decorator._handlers.Should().ContainInOrder( expected );
        }
    }
}
