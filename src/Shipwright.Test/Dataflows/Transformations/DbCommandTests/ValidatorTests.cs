// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture.Xunit2;
using Shipwright.Databases;

namespace Shipwright.Dataflows.Transformations.DbCommandTests;

public class ValidatorTests
{
    DbCommand instance = new Fixture().WithDataflowCustomization().Create<DbCommand>();
    readonly IValidator<DbCommand> validator = new DbCommand.Validator();

    public class Sql : ValidatorTests
    {
        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        public async Task invalid_when_null_or_whitespace( string value )
        {
            instance = instance with { Sql = value };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Sql );
        }

        [Theory]
        [AutoData]
        public async Task valid_when_given( string value )
        {
            instance = instance with { Sql = value };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Sql );
        }
    }

    public class ConnectionInfo : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { ConnectionInfo = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.ConnectionInfo );
        }

        [Fact]
        public async Task valid_when_given()
        {
            instance = instance with { ConnectionInfo = new FakeDbConnectionInfo() };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.ConnectionInfo );
        }
    }

    public class Input : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { Input = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Input );
        }

        [Fact]
        public async Task invalid_when_item_null()
        {
            instance.Input.Add( null! );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Input );
        }

        [Theory]
        [InlineData(null)]
        [WhitespaceCases]
        public async Task invalid_when_field_null_or_whitespace( string field )
        {
            instance.Input.Add( new( field ) );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( $"{nameof(instance.Input)}[{instance.Input.Count - 1}].{nameof(DbLookup.FieldMap.Field)}" );
        }

        [Fact]
        public async Task valid_when_empty()
        {
            instance.Input.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Input );
        }

        [Theory]
        [AutoData]
        public async Task valid_when_given_elements( string field )
        {
            instance.Input.Add( new( field ) );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Input );
        }
    }

    public class Parameters : ValidatorTests
    {
        [Fact]
        public async Task invalid_when_null()
        {
            instance = instance with { Parameters = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Parameters );
        }

        [Fact]
        public async Task valid_when_empty()
        {
            instance.Parameters.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Parameters );
        }

        [Theory]
        [AutoData]
        public async Task valid_when_given_elements( KeyValuePair<string,object?> kvp )
        {
            instance.Parameters.Add( kvp );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Parameters );
        }
    }
}
