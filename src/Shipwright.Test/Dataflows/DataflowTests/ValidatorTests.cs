// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture.Xunit2;

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
}