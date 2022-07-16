// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture.Xunit2;
using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.ConversionTests;

public class UpperCaseTests
{
    object value = new Fixture().Create<string>();
    object? converted;
    bool method() => Conversion.ToUpperCase( value, out converted );

    public class WhenNotString : UpperCaseTests
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

    public class WhenString : UpperCaseTests
    {
        [Theory]
        [WhitespaceCases]
        [AutoData]
        public void capitalizes_and_returns_true( string input )
        {
            value = input.ToLowerInvariant();
            var actual = method();
            actual.Should().BeTrue();
            converted.Should().Be( input.ToUpperInvariant() );
        }
    }
}
