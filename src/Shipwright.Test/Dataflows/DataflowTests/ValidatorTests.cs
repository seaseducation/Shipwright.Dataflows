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
}
