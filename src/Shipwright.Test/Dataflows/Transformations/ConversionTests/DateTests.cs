// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.ConversionTests;

public class DateTests
{
    object value = new Fixture().Create<string>();
    object? converted;
    bool method() => Conversion.ToDate( value, out converted );

    public class WhenValueNotConvertible : DateTests
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

    public class WhenValueConvertible : DateTests
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public class ConvertibleCases : TheoryData<object, object>
        {
            public ConvertibleCases()
            {
                // direct conversion
                Add( DateTime.MinValue, DateTime.MinValue.Date );
                Add( DateTime.MaxValue, DateTime.MaxValue.Date );

                // parsing
                Add( "2018-01-02 03:04:05", new DateTime( 2018, 01, 02 ) );
                Add( "Jan  2, 2018", new DateTime( 2018, 01, 02 ) );
                Add( DateTime.MinValue.ToString( "o" ), DateTime.MinValue.Date );
                Add( DateTime.MaxValue.ToString( "o" ), DateTime.MaxValue.Date );
            }
        }

        [Theory]
        [ClassData(typeof(ConvertibleCases))]
        public void converts_value_drops_time_and_returns_true( object input, DateTime expected )
        {
            value = input;
            var success = method();
            success.Should().BeTrue();
            converted.Should().Be( expected );
        }
    }
}
