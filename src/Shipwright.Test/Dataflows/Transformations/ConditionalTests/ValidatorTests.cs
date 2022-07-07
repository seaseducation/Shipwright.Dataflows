// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.ConditionalTests;

public class ValidatorTests
{
    Conditional instance = new Fixture().WithDataflowCustomization().Create<Conditional>();
    readonly IValidator<Conditional> validator = new Conditional.Validator();

    public class When : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { When = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.When );
        }

        [Fact]
        public async Task valid_when_given()
        {
            instance = instance with { When = ( _, _ ) => Task.FromResult( true ) };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.When );
        }
    }

    public class Transformations : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { Transformations = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Transformations );
        }

        [Fact]
        public async Task invalid_when_empty()
        {
            instance.Transformations.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Transformations );
        }

        [Fact]
        public async Task invalid_when_contains_null_element()
        {
            instance.Transformations.Add( null! );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Transformations );
        }

        [Fact]
        public async Task valid_when_given_elements()
        {
            instance = instance with { Transformations = new Fixture().WithDataflowCustomization().CreateMany<Transformation>().ToList() };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Transformations );
        }
    }
}
