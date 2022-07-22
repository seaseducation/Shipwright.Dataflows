// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.DbUpsertTests;

public class ValidatorTests
{
    readonly Fixture _fixture = new Fixture().WithDataflowCustomization();
    DbUpsert instance;
    readonly IValidator<DbUpsert> validator = new DbUpsert.Validator();

    public ValidatorTests()
    {
        instance = _fixture.Create<DbUpsert>();
    }

    public class ConnectionInfo : ValidatorTests
    {
        [Fact]
        public async Task cannot_be_null()
        {
            instance = instance with { ConnectionInfo = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.ConnectionInfo );
        }

        [Fact]
        public async Task valid_when_given()
        {
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.ConnectionInfo );
        }
    }

    public class Table : ValidatorTests
    {
        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        public async Task cannot_be_null_or_whitespace( string value )
        {
            instance = instance with { Table = value };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Table );
        }

        [Fact]
        public async Task valid_when_given()
        {
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Table );
        }
    }

    public class Fields : ValidatorTests
    {
        [Fact]
        public async Task cannot_be_null()
        {
            instance = instance with { Fields = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Fact]
        public async Task cannot_be_empty()
        {
            instance = instance with { Fields = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Fact]
        public async Task cannot_have_null_elements()
        {
            instance.Fields.Add( null! );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        public async Task cannot_have_null_or_whitespace_fields( string value )
        {
            instance.Fields.Add( _fixture.Create<DbUpsert.FieldMap>() with { Field = value } );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        public async Task cannot_have_null_or_whitespace_columns( string value )
        {
            instance.Fields.Add( _fixture.Create<DbUpsert.FieldMap>() with { Column = value } );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Fact]
        public async Task cannot_have_null_replace_delegate()
        {
            instance.Fields.Add( _fixture.Create<DbUpsert.FieldMap>() with { Replace = null! } );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Fact]
        public async Task requires_a_key_column()
        {
            var keys = instance.Fields.Where( _ => _.Type == DbUpsert.ColumnType.Key ).ToArray();

            foreach ( var key in keys )
                instance.Fields.Remove( key );

            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Fact]
        public async Task cannot_have_duplicate_column()
        {
            instance.Fields.Add( _fixture.Create<DbUpsert.FieldMap>() with { Column = instance.Fields.First().Column } );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.Fields );
        }

        [Fact]
        public async Task valid_when_has_at_least_one_key_and_unique_column_names()
        {
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.Fields );
        }
    }

    public class BeforeInsert : ValidatorTests
    {
        [Fact]
        public async Task cannot_be_null()
        {
            instance = instance with { BeforeInsert = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.BeforeInsert );
        }

        [Fact]
        public async Task cannot_contain_null_elements()
        {
            instance.BeforeInsert.Add( null! );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.BeforeInsert );
        }

        [Fact]
        public async Task valid_when_empty()
        {
            instance.BeforeInsert.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.BeforeInsert );
        }

        [Fact]
        public async Task valid_when_has_elements()
        {
            instance.BeforeInsert.Add( new FakeTransformation() );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.BeforeInsert );
        }
    }

    public class AfterInsert : ValidatorTests
    {
        [Fact]
        public async Task cannot_be_null()
        {
            instance = instance with { AfterInsert = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.AfterInsert );
        }

        [Fact]
        public async Task cannot_contain_null_elements()
        {
            instance.AfterInsert.Add( null! );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.AfterInsert );
        }

        [Fact]
        public async Task valid_when_empty()
        {
            instance.AfterInsert.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.AfterInsert );
        }

        [Fact]
        public async Task valid_when_has_elements()
        {
            instance.AfterInsert.Add( new FakeTransformation() );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.AfterInsert );
        }
    }

    public class BeforeUpdate : ValidatorTests
    {
        [Fact]
        public async Task cannot_be_null()
        {
            instance = instance with { BeforeUpdate = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.BeforeUpdate );
        }

        [Fact]
        public async Task cannot_contain_null_elements()
        {
            instance.BeforeUpdate.Add( null! );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.BeforeUpdate );
        }

        [Fact]
        public async Task valid_when_empty()
        {
            instance.BeforeUpdate.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.BeforeUpdate );
        }

        [Fact]
        public async Task valid_when_has_elements()
        {
            instance.BeforeUpdate.Add( new FakeTransformation() );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.BeforeUpdate );
        }
    }

    public class AfterUpdate : ValidatorTests
    {
        [Fact]
        public async Task cannot_be_null()
        {
            instance = instance with { AfterUpdate = null! };
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.AfterUpdate );
        }

        [Fact]
        public async Task cannot_contain_null_elements()
        {
            instance.AfterUpdate.Add( null! );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldHaveValidationErrorFor( _ => _.AfterUpdate );
        }

        [Fact]
        public async Task valid_when_empty()
        {
            instance.AfterUpdate.Clear();
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.AfterUpdate );
        }

        [Fact]
        public async Task valid_when_has_elements()
        {
            instance.AfterUpdate.Add( new FakeTransformation() );
            var result = await validator.TestValidateAsync( instance );
            result.ShouldNotHaveValidationErrorFor( _ => _.AfterUpdate );
        }
    }
}
