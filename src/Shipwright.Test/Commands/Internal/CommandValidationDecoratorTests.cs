// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;

namespace Shipwright.Commands.Internal;

public abstract class CommandValidationDecoratorTests
{
    Mock<ICommandHandler<FakeCommand, Guid>> inner = new( MockBehavior.Strict );
    Mock<IValidator<FakeCommand>> validator = new( MockBehavior.Strict );
    ICommandHandler<FakeCommand, Guid> instance() => new CommandValidationDecorator<FakeCommand, Guid>( inner?.Object!, validator?.Object! );

    public class Constructor : CommandValidationDecoratorTests
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

    public abstract class Execute : CommandValidationDecoratorTests
    {
        FakeCommand command = new();
        CancellationToken cancellationToken;
        Task<Guid> method() => instance().Execute( command, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_command( bool canceled )
        {
            cancellationToken = new( canceled );
            command = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(command), method );
        }

        public class WhenNotValid : Execute
        {
            [Theory]
            [BooleanCases]
            public async Task throws_validation( bool canceled )
            {
                cancellationToken = new( canceled );

                var errors = new Fixture().CreateMany<ValidationFailure>().ToList();
                var result = new ValidationResult( errors );
                validator.Setup( _ => _.ValidateAsync( command, cancellationToken ) ).ReturnsAsync( result );

                var ex = await Assert.ThrowsAsync<ValidationException>( method );
                ex.Errors.Should().BeEquivalentTo( errors );
            }
        }

        public class WhenValid : Execute
        {
            [Theory]
            [BooleanCases]
            public async Task returns_result_from_executed_inner_handler( bool canceled )
            {
                cancellationToken = new( canceled );

                var errors = Array.Empty<ValidationFailure>();
                var result = new ValidationResult( errors );
                validator.Setup( _ => _.ValidateAsync( command, cancellationToken ) ).ReturnsAsync( result );

                var expected = Guid.NewGuid();
                inner.Setup( _ => _.Execute( command, cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                actual.Should().Be( expected );
            }
        }
    }
}
