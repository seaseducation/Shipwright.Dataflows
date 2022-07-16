// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture.Xunit2;
using Shipwright.Dataflows.EventSinks;
using Shipwright.Dataflows.Transformations;

namespace Shipwright.Dataflows.DataflowTests;

public class ValidatorTests
{
    Dataflow instance = new();
    readonly IValidator<Dataflow> validator = new Dataflow.Validator();

    public class Name : ValidatorTests
    {
        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        public async Task cannot_be_empty( string value )
        {
            instance = instance with { Name = value };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Name );
        }

        [Theory]
        [AutoData]
        public async Task valid_when_given( string value )
        {
            instance = instance with { Name = value };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Name );
        }
    }

    public class MaxDegreeOfParallelism : ValidatorTests
    {
        [Theory]
        [InlineData( -3 )]
        [InlineData( -2 )]
        [InlineData( 0 )]
        public async Task cannot_be_negative_when_not_unlimited( int value )
        {
            instance = instance with { MaxDegreeOfParallelism = value };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.MaxDegreeOfParallelism );
        }

        [Theory]
        [InlineData( -1 )] // unlimited
        [InlineData( 1 )]
        [InlineData( 2 )]
        public async Task valid_when_positive( int value )
        {
            instance = instance with { MaxDegreeOfParallelism = value };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.MaxDegreeOfParallelism );
        }
    }

    public class Sources : ValidatorTests
    {
        [Fact]
        public async Task cannot_be_null()
        {
            instance = instance with { Sources = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Sources );
        }

        [Fact]
        public async Task cannot_be_empty()
        {
            instance.Sources.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Sources );
        }

        [Fact]
        public async Task valid_when_given_contents()
        {
            instance = new() { Sources = { new FakeSource() } };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Sources );
        }
    }

    public class Transformations : ValidatorTests
    {
        [Fact]
        public async Task cannot_be_null()
        {
            instance = instance with { Transformations = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Transformations );
        }

        [Fact]
        public async Task cannot_be_empty()
        {
            instance.Transformations.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Transformations );
        }

        [Fact]
        public async Task valid_when_given_contents()
        {
            instance = new() { Transformations = { new FakeTransformation() } };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Transformations );
        }
    }

    public class EventSinks : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = new() { EventSinks = null! };
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
        public async Task valid_when_given_contents()
        {
            instance = new() { EventSinks = { new FakeEventSink() } };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.EventSinks );
        }
    }
}
