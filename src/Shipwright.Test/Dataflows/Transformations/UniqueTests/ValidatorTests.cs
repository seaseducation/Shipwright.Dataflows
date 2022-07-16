// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.UniqueTests;

public class ValidatorTests
{
    Unique instance = new Fixture().Create<Unique>();
    readonly IValidator<Unique> validator = new Unique.Validator();

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

        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        public async Task invalid_when_contains_empty_entry( string value )
        {
            instance.Fields.Add( value );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( $"{nameof(instance.Fields)}[{instance.Fields.Count - 1}]" );
        }

        [Fact]
        public async Task valid_when_given_entries()
        {
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Fields );
        }
    }

    public class OnFailure : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { OnFailure = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.OnFailure );
        }

        [Fact]
        public async Task valid_when_given()
        {
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.OnFailure );
        }
    }
}
