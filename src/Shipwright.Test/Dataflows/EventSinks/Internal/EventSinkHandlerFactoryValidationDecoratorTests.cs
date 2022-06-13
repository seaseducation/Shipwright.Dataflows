// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;

namespace Shipwright.Dataflows.EventSinks.Internal;

public class EventSinkHandlerFactoryValidationDecoratorTests
{
    Mock<IEventSinkHandlerFactory<FakeEventSink>> inner = new( MockBehavior.Strict );
    Mock<IValidator<FakeEventSink>> validator = new( MockBehavior.Strict );
    IEventSinkHandlerFactory<FakeEventSink> instance() => new EventSinkHandlerFactoryValidationDecorator<FakeEventSink>( inner?.Object!, validator?.Object! );

    public class Constructor : EventSinkHandlerFactoryValidationDecoratorTests
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

    public abstract class Create : EventSinkHandlerFactoryValidationDecoratorTests
    {
        FakeEventSink eventSink = new();
        Dataflow dataflow = new();
        CancellationToken cancellationToken;
        Task<IEventSinkHandler> method() => instance().Create( eventSink, dataflow, cancellationToken );

        readonly Fixture _fixture = new();

        [Theory]
        [BooleanCases]
        public async Task requires_eventSink( bool canceled )
        {
            cancellationToken = new( canceled );
            eventSink = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(eventSink), method );
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

                var errors = _fixture.CreateMany<ValidationFailure>().ToArray();
                var result = new ValidationResult( errors );
                validator.Setup( _ => _.ValidateAsync( eventSink, cancellationToken ) ).ReturnsAsync( result );

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

                var expected = new Mock<IEventSinkHandler>( MockBehavior.Strict ).Object;
                var errors = Array.Empty<ValidationFailure>();
                var result = new ValidationResult( errors );
                validator.Setup( _ => _.ValidateAsync( eventSink, cancellationToken ) ).ReturnsAsync( result );
                inner.Setup( _ => _.Create( eventSink, dataflow, cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                actual.Should().Be( expected );
            }
        }
    }
}
