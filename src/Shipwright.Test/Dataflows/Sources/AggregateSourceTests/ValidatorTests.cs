// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;

namespace Shipwright.Dataflows.Sources.AggregateSourceTests;

public class ValidatorTests
{
    AggregateSource instance = new();
    IValidator<AggregateSource> validator = new AggregateSource.Validator();

    public class Sources : ValidatorTests
    {
        [Fact]
        public async Task cannot_be_null()
        {
            instance = instance with { Sources = null! };
            var actual = await validator.TestValidateAsync( instance );
            actual.ShouldHaveValidationErrorFor( _ => _.Sources );
        }

        [Fact]
        public async Task cannot_be_empty()
        {
            instance.Sources.Clear();
            var actual = await validator.TestValidateAsync( instance );
            actual.ShouldHaveValidationErrorFor( _ => _.Sources );
        }

        [Fact]
        public async Task valid_when_has_contents()
        {
            instance = new() { Sources = { new FakeSource() } };
            var actual = await validator.TestValidateAsync( instance );
            actual.ShouldNotHaveValidationErrorFor( _ => _.Sources );
        }
    }

    public class Events : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = new() { Events = null! };
            var actual = await validator.TestValidateAsync( instance );
            actual.ShouldHaveValidationErrorFor( _ => _.Events );
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public async Task valid_when_empty_or_has_contents( int events )
        {
            instance = new() { Events = new Fixture().CreateMany<LogEvent>( events ).ToList() };
            var actual = await validator.TestValidateAsync( instance );
            actual.ShouldNotHaveValidationErrorFor( _ => _.Events );
        }
    }
}
