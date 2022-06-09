// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;

namespace Shipwright.Dataflows.Transformations.Internal;

public class TransformationHandlerFactoryValidationDecoratorTests
{
    Mock<ITransformationHandlerFactory<FakeTransformation>> inner = new( MockBehavior.Strict );
    Mock<IValidator<FakeTransformation>> validator = new( MockBehavior.Strict );
    ITransformationHandlerFactory<FakeTransformation> instance() => new TransformationHandlerFactoryValidationDecorator<FakeTransformation>( inner?.Object!, validator?.Object! );

    public class Constructor : TransformationHandlerFactoryValidationDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }

        [Fact]
        public void requires_validator()
        {
            validator = null!;
            Assert.Throws<ArgumentNullException>( nameof(validator), instance );
        }
    }

    public abstract class Create : TransformationHandlerFactoryValidationDecoratorTests
    {
        FakeTransformation transformation = new();
        CancellationToken cancellationToken;
        Task<ITransformationHandler> method() => instance().Create( transformation, cancellationToken );

        readonly Fixture _fixture = new();

        [Theory]
        [BooleanCases]
        public async Task requires_transformation( bool canceled )
        {
            cancellationToken = new( canceled );
            transformation = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(transformation), method );
        }

        public class WhenNotValid : Create
        {
            [Theory]
            [BooleanCases]
            public async Task throws_validation( bool canceled )
            {
                cancellationToken = new( canceled );

                var errors = _fixture.CreateMany<ValidationFailure>().ToArray();
                var result = new ValidationResult( errors );
                validator.Setup( _ => _.ValidateAsync( transformation, cancellationToken ) ).ReturnsAsync( result );

                var ex = await Assert.ThrowsAsync<ValidationException>( method );
                ex.Errors.Should().ContainInOrder( errors );
            }
        }

        public class WhenValid : Create
        {
            [Theory]
            [BooleanCases]
            public async Task returns_handler_from_inner_factory( bool canceled )
            {
                cancellationToken = new( canceled );

                var expected = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
                var errors = Array.Empty<ValidationFailure>();
                var result = new ValidationResult( errors );
                validator.Setup( _ => _.ValidateAsync( transformation, cancellationToken ) ).ReturnsAsync( result );
                inner.Setup( _ => _.Create( transformation, cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                actual.Should().Be( expected );
            }
        }
    }
}
