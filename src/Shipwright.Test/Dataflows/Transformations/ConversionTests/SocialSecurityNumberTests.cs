// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.ConversionTests;

public class SocialSecurityNumberTests
{
    object value = new Fixture().Create<string>();
    object? converted;
    bool method() => Conversion.ToSocialSecurityNumber( value, out converted );

    public class WhenNotString : SocialSecurityNumberTests
    {
        [Fact]
        public void returns_false()
        {
            value = Guid.NewGuid();
            var actual = method();
            actual.Should().BeFalse();
            converted.Should().BeNull();
        }
    }

    public class WhenStringWithoutNineDigits : SocialSecurityNumberTests
    {
        [Theory]
        [InlineData("12345678")]
        [InlineData("123-456-7890")]
        // ReSharper disable once StringLiteralTypo
        [InlineData("ABCDEFGHI")]
        public void returns_false( string input )
        {
            value = input;
            var actual = method();
            actual.Should().BeFalse();
            converted.Should().BeNull();
        }
    }

    public class WhenStringHasNineDigits : SocialSecurityNumberTests
    {
        [Theory]
        [InlineData("123456789", "123-45-6789")]
        [InlineData("123-45-6789", "123-45-6789")]
        [InlineData("98-7654321", "987-65-4321")]
        public void returns_false( string input, string expected )
        {
            value = input;
            var actual = method();
            actual.Should().BeTrue();
            converted.Should().Be( expected );
        }
    }
}
