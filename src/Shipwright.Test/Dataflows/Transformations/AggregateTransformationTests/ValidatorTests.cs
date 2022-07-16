// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;

namespace Shipwright.Dataflows.Transformations.AggregateTransformationTests;

public class ValidatorTests
{
    AggregateTransformation instance = new();
    IValidator<AggregateTransformation> validator = new AggregateTransformation.Validator();

    public class Transformation : ValidatorTests
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
        public async Task valid_when_contains_transformations()
        {
            instance.Transformations.Add( new FakeTransformation() );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Transformations );
        }
    }
}
