// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.TruncateTests;

public class ValidatorTests
{
    Truncate instance = new Fixture().Create<Truncate>();
    IValidator<Truncate> validator = new Truncate.Validator();

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
        public async Task invalid_when_field_name_null_or_whitespace( string field )
        {
            instance.Fields.Add( new( field, 1 ) );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( $"{nameof(instance.Fields)}[{instance.Fields.Count - 1}].{nameof(Truncate.Setting.Field)}" );
        }

        [Theory]
        [InlineData( 0 )]
        [InlineData( -1 )]
        [InlineData( int.MinValue )]
        public async Task invalid_when_length_not_positive( int length )
        {
            instance.Fields.Add( new( new Fixture().Create<string>(), length ) );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( $"{nameof(instance.Fields)}[{instance.Fields.Count - 1}].{nameof(Truncate.Setting.Length)}" );
        }
    }
}
