// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.EventSinks.AggregateEventSinkTests;

public class ValidatorTests
{
    AggregateEventSink instance = new();
    IValidator<AggregateEventSink> validator = new AggregateEventSink.Validator();

    public class EventSinks : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { EventSinks = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.EventSinks );
        }

        [Fact]
        public async Task invalid_when_empty()
        {
            instance.EventSinks.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.EventSinks );
        }

        [Fact]
        public async Task valid_when_contains_elements()
        {
            instance.EventSinks.Add( new FakeEventSink() );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.EventSinks );
        }
    }
}
