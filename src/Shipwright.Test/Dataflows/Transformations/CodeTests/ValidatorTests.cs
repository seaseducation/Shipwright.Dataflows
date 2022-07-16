// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.CodeTests;

public class ValidatorTests
{
    Code instance = new() { Delegate = ( record, ct ) => Task.CompletedTask };
    readonly IValidator<Code> validator = new Code.Validator();

    public class Delegate : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { Delegate = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Delegate );
        }

        [Fact]
        public async Task valid_when_given()
        {
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Delegate );
        }
    }
}
