// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.DefaultValueTests;

public class ValidatorTests
{
    DefaultValue instance = new Fixture().Create<DefaultValue>();
    IValidator<DefaultValue> validator = new DefaultValue.Validator();

    public class Defaults : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { Defaults = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Defaults );
        }

        [Fact]
        public async Task invalid_when_empty()
        {
            instance.Defaults.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Defaults );
        }

        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        public async Task invalid_when_child_field_null_or_whitespace( string value )
        {
            instance.Defaults.Add( new( value, () => 0 ) );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( $"{nameof(instance.Defaults)}[{instance.Defaults.Count - 1}].{nameof(DefaultValue.Setting.Field)}" );
        }

        [Fact]
        public async Task invalid_when_child_value_delegate_null()
        {
            instance.Defaults.Add( new( "blah", null! ) );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( $"{nameof(instance.Defaults)}[{instance.Defaults.Count - 1}].{nameof(DefaultValue.Setting.Value)}" );
        }
    }
}
