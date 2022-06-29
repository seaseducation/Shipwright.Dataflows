// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.ReplaceTests;

public abstract class ValidatorTests
{
    Replace instance = new Fixture().Create<Replace>();
    readonly IValidator<Replace> validator = new Replace.Validator();

    [Fact]
    public async Task valid_when_all_properties_given()
    {
        var result = await validator.TestValidateAsync( instance );
        result.ShouldNotHaveAnyValidationErrors();
    }

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
        public async Task invalid_when_contains_empty_element( string field )
        {
            instance.Fields.Add( field );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( $"{nameof(Replace.Fields)}[{instance.Fields.Count - 1}]" );
        }
    }

    public class Replacements : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { Replacements = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Replacements );
        }

        [Fact]
        public async Task invalid_when_empty()
        {
            instance.Replacements.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Replacements );
        }

        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        public async Task invalid_when_incoming_value_empty( string field )
        {
            instance.Replacements.Add( new( field, Guid.NewGuid() ) );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( $"{nameof(Replace.Replacements)}[{instance.Replacements.Count - 1}].{nameof(Replace.Replacement.Incoming)}" );
        }
    }
}
