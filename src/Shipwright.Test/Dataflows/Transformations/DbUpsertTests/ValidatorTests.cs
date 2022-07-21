// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.DbUpsertTests;

public class ValidatorTests
{
    DbUpsert instance = new Fixture().WithDataflowCustomization().Create<DbUpsert>();
    readonly IValidator<DbUpsert> validator = new DbUpsert.Validator();

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
}
