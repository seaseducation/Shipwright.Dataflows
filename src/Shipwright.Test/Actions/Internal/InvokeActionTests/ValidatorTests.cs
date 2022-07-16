// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture.Xunit2;

namespace Shipwright.Actions.Internal.InvokeActionTests;

public class ValidatorTests
{
    InvokeAction instance = new();
    TestValidationResult<InvokeAction> validation() => new InvokeAction.Validator().TestValidate( instance );

    public class Action : ValidatorTests
    {
        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        public void invalid_when_null_or_whitespace( string value )
        {
            instance = new() { Action = value };
            validation().ShouldHaveValidationErrorFor( _ => _.Action );
        }

        [Theory]
        [AutoData]
        public void valid_when_given( string value )
        {
            instance = new() { Action = value };
            validation().ShouldNotHaveValidationErrorFor( _ => _.Action );
        }
    }

    public class Context : ValidatorTests
    {
        [Fact]
        public void invalid_when_null()
        {
            instance = new() { Context = null! };
            validation().ShouldHaveValidationErrorFor( _ => _.Context );
        }

        [Fact]
        public void valid_when_given()
        {
            instance = new() { Context = new() };
            validation().ShouldNotHaveValidationErrorFor( _ => _.Context );
        }
    }
}
