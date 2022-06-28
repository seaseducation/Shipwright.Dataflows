// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.ConversionTests;

public class DateTimeTests
{
    object value = new Fixture().Create<string>();
    object? converted;
    bool method() => Conversion.ToDateTime( value, out converted );

    public class WhenValueNotConvertible : DateTimeTests
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public class InconvertibleCases : TheoryData<object>
        {
            public InconvertibleCases()
            {
                // text that can't be parsed
                Add( Guid.NewGuid().ToString() );

                // out-of-range
                Add( long.MaxValue );
                Add( long.MinValue );

                // inconvertible types
                Add( Guid.NewGuid() );
            }
        }

        [Theory]
        [InlineData( null )]
        [WhitespaceCases]
        [ClassData(typeof(InconvertibleCases))]
        public void returns_false( object input )
        {
            value = input;
            var success = method();
            success.Should().BeFalse();
            converted.Should().BeNull();
        }
    }

    public class WhenValueConvertible : DateTimeTests
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public class ConvertibleCases : TheoryData<object, object>
        {
            public ConvertibleCases()
            {
                // direct type conversion
                Add( DateTime.MinValue, DateTime.MinValue );
                Add( DateTime.MaxValue, DateTime.MaxValue );

                // parsing
                Add( "2018-01-02 03:04:05", new DateTime( 2018, 01, 02, 03, 04, 05 ) );
                Add( "Jan  2, 2018 12:30 AM", new DateTime( 2018, 01, 02, 00, 30, 00 ) );
                Add( DateTime.MinValue.ToString( "o" ), DateTime.MinValue );
                Add( DateTime.MaxValue.ToString( "o" ), DateTime.MaxValue );
            }
        }

        [Theory]
        [ClassData(typeof(ConvertibleCases))]
        public void converts_value_and_returns_true( object input, DateTime expected )
        {
            value = input;
            var success = method();
            success.Should().BeTrue();
            converted.Should().Be( expected );
        }
    }
}
