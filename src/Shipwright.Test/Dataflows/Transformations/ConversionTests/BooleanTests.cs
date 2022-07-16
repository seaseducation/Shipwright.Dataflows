// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.ConversionTests;

public class BooleanTests
{
    object value = new Fixture().Create<string>();
    object? converted;
    bool method() => Conversion.ToBoolean( value, out converted );

    public class WhenConvertible : BooleanTests
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public class ConvertibleCases : TheoryData<object, bool>
        {
            public ConvertibleCases()
            {
                // exact types
                Add( true, true );
                Add( false, false );

                // parsed text
                Add( "true", true );
                Add( "True", true );
                Add( "TRUE", true );
                Add( "false", false );
                Add( "FALSE", false );
                Add( "False", false );
                Add( "yes", true );
                Add( "YES", true );
                Add( "Y", true );
                Add( "no", false );
                Add( "NO", false );
                Add( "N", false );
                Add( "1", true );
                Add( "0", false );

                // convertible
                Add( 1, true );
                Add( -1, true );
                Add( 0, false );
            }
        }

        [Theory]
        [ClassData(typeof(ConvertibleCases))]
        public void converts_value_and_returns_true( object input, bool expected )
        {
            value = input;
            var success = method();
            success.Should().BeTrue();
            converted.Should().Be( expected );
        }
    }

    public class WhenNotConvertible : BooleanTests
    {
        // ReSharper disable once MemberCanBePrivate.Global
        public class InconvertibleCases : TheoryData<object>
        {
            public InconvertibleCases()
            {
                // text that can't be parsed
                Add( Guid.NewGuid().ToString() );

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
}
