// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Microsoft.Extensions.Logging;

namespace Shipwright.Dataflows.Transformations.ConversionTests;

public class ValidatorTests
{
    static bool fakeConverter( object value, out object? converted )
    {
        converted = null;
        return false;
    }

    static LogEvent fakeOnFailure( string field ) => new( true, LogLevel.Critical, "Conversion failed" );

    Conversion instance = new()
    {
        Fields = new Fixture().CreateMany<string>().ToList(),
        Converter = fakeConverter,
        OnFailed = fakeOnFailure
    };
    IValidator<Conversion> validator = new Conversion.Validator();

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
        public async Task invalid_when_any_field_null_or_whitespace( string field )
        {
            instance.Fields.Add( field );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( $"{nameof(instance.Fields)}[{instance.Fields.Count -1}]" );
        }

        [Fact]
        public async Task valid_when_contains_given_elements()
        {
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Fields );
        }
    }

    public class Converter : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { Converter = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Converter );
        }

        [Fact]
        public async Task invalid_when_given()
        {
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Converter );
        }
    }

    public class OnFailed : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { OnFailed = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.OnFailed );
        }

        [Fact]
        public async Task invalid_when_given()
        {
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.OnFailed );
        }
    }
}
