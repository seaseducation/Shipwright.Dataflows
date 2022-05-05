// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentValidation;
using FluentValidation.Results;

namespace Shipwright.Core.Commands.Decorators;

public abstract class ValidationCommandDecoratorTests
{
    Mock<ICommandHandler<FakeCommand, Guid>> inner = new( MockBehavior.Strict );
    Mock<IValidator<FakeCommand>> validator = new( MockBehavior.Strict );
    ICommandHandler<FakeCommand, Guid> instance() => new ValidationCommandDecorator<FakeCommand, Guid>( inner?.Object!, validator?.Object! );

    public class Constructor : ValidationCommandDecoratorTests
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

    public abstract class Execute : ValidationCommandDecoratorTests
    {
        FakeCommand command = new();
        CancellationToken cancellationToken;
        Task<Guid> method() => instance().Execute( command, cancellationToken );

        [Fact]
        public async Task requires_command()
        {
            command = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(command), method );
        }

        public class WhenCommandIsValid : Execute
        {
            [Theory]
            [BooleanCases]
            public async Task returns_result_from_executed_inner_handler( bool canceled )
            {
                cancellationToken = new( canceled );

                var success = new ValidationResult( Array.Empty<ValidationFailure>() );
                validator.Setup( _ => _.ValidateAsync( command, cancellationToken ) ).ReturnsAsync( success );

                var expected = Guid.NewGuid();
                inner.Setup( _ => _.Execute( command, cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                actual.Should().Be( expected );
            }
        }

        public class WhenCommandIsNotValid : Execute
        {
            [Theory]
            [BooleanCases]
            public async Task throws_validation( bool canceled )
            {
                cancellationToken = new( canceled );

                var errors = new Fixture().CreateMany<ValidationFailure>().ToArray();
                var failure = new ValidationResult( errors );
                validator.Setup( _ => _.ValidateAsync( command, cancellationToken ) ).ReturnsAsync( failure );

                var ex = await Assert.ThrowsAsync<ValidationException>( method );
                ex.Errors.Should().BeEquivalentTo( errors );
            }
        }
    }
}
