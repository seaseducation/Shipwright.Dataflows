// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.ConversionTests;

public class DecimalTests
{
    object value = new Fixture().Create<string>();
    object? converted;
    bool method() => Conversion.ToDecimal( value, out converted );

    public class WhenValueNotConvertible : DecimalTests
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public class InconvertibleCases : TheoryData<object>
        {
            public InconvertibleCases()
            {
                // text that can't be parsed
                Add( Guid.NewGuid().ToString() );

                // out-of-range
                Add( double.MaxValue );
                Add( double.MinValue );

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

    public class WhenValueConvertible : DecimalTests
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public class ConvertibleCases : TheoryData<object, object>
        {
            public ConvertibleCases()
            {
                // direct type conversion
                Add( decimal.MinValue, decimal.MinValue );
                Add( decimal.MaxValue, decimal.MaxValue );

                // convertible cases
                Add( int.MinValue, (decimal)int.MinValue );
                Add( int.MaxValue, (decimal)int.MaxValue );

                // parsing
                Add( "123456789", 123456789M );
                Add( "987,654,321", 987654321M );
                Add( "1,234,567.89", 1234567.89M );
                Add( "98,765.4321", 98765.4321M );
            }
        }

        [Theory]
        [ClassData(typeof(ConvertibleCases))]
        public void converts_value_and_returns_true( object input, decimal expected )
        {
            value = input;
            var success = method();
            success.Should().BeTrue();
            converted.Should().Be( expected );
        }
    }
}
