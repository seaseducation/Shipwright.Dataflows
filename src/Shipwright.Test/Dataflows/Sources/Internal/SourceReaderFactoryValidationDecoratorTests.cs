// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;

namespace Shipwright.Dataflows.Sources.Internal;

public abstract class SourceReaderFactoryValidationDecoratorTests
{
    Mock<ISourceReaderFactory<FakeSource>> inner = new( MockBehavior.Strict );
    Mock<IValidator<FakeSource>> validator = new( MockBehavior.Strict );
    ISourceReaderFactory<FakeSource> instance() => new SourceReaderFactoryValidationDecorator<FakeSource>( inner?.Object!, validator?.Object! );

    public class Constructor : SourceReaderFactoryValidationDecoratorTests
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

    public abstract class Create : SourceReaderFactoryValidationDecoratorTests
    {
        FakeSource source = new();
        Dataflow dataflow = new();
        CancellationToken cancellationToken;
        Task<ISourceReader> method() => instance().Create( source, dataflow, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_source( bool canceled )
        {
            cancellationToken = new( canceled );
            source = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(source), method );
        }

        [Theory]
        [BooleanCases]
        public async Task requires_dataflow( bool canceled )
        {
            cancellationToken = new( canceled );
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        public class WhenNotValid : Create
        {
            [Theory]
            [BooleanCases]
            public async Task throws_validation( bool canceled )
            {
                cancellationToken = new( canceled );

                var failures = new Fixture().CreateMany<ValidationFailure>().ToArray();
                var result = new ValidationResult( failures );
                validator.Setup( _ => _.ValidateAsync( source, cancellationToken ) ).ReturnsAsync( result );

                var ex = await Assert.ThrowsAsync<ValidationException>( method );
                ex.Errors.Should().BeEquivalentTo( failures );
            }
        }

        public class WhenValid : Create
        {
            [Theory]
            [BooleanCases]
            public async Task throws_validation( bool canceled )
            {
                cancellationToken = new( canceled );

                var failures = Array.Empty<ValidationFailure>();
                var result = new ValidationResult( failures );
                validator.Setup( _ => _.ValidateAsync( source, cancellationToken ) ).ReturnsAsync( result );

                var expected = new Mock<ISourceReader>( MockBehavior.Strict ).Object;
                inner.Setup( _ => _.Create( source, dataflow, cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                actual.Should().Be( expected );
            }
        }
    }
}
