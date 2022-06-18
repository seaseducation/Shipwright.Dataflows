// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;

namespace Shipwright.Dataflows.Transformations.RequiredTests;

public class ValidatorTests
{
    Required instance = new();
    readonly IValidator<Required> validator = new Required.Validator();

    public class Fields : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { Fields = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Fact]
        public async Task invalid_when_empty()
        {
            instance.Fields.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Fact]
        public async Task valid_when_given_content()
        {
            instance = instance with { Fields = new Fixture().CreateMany<string>().ToList() };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Fields );
        }
    }

    public class OnError : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { OnError = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.OnError );
        }

        [Fact]
        public async Task valid_when_given()
        {
            // defaults to non-null
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.OnError );
        }
    }
}
