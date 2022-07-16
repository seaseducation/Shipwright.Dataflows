// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Shipwright.Dataflows.Sources;

namespace Shipwright.Dataflows;

public abstract class RecordTests
{
    Dictionary<string, object?> data = new Dictionary<string, object?>();
    Dataflow dataflow = new();
    Source source = new FakeSource();
    long position;
    Record instance() => new( data, dataflow, source, position );

    protected RecordTests()
    {
        var fixture = new Fixture();
        position = fixture.Create<long>();

        for ( var i = 0; i < 3; i++ )
            data[fixture.Create<string>()] = fixture.Create<string>();
    }

    public class Constructor : RecordTests
    {
        [Fact]
        public void requires_data()
        {
            data = null!;
            Assert.Throws<ArgumentNullException>( nameof(data), instance );
        }

        [Fact]
        public void requires_dataflow()
        {
            dataflow = null!;
            Assert.Throws<ArgumentNullException>( nameof(dataflow), instance );
        }

        [Fact]
        public void requires_source()
        {
            source = null!;
            Assert.Throws<ArgumentNullException>( nameof(source), instance );
        }
    }

    public static IEnumerable<object[]> StringComparerCases() => Enum.GetValues<StringComparison>()
            .Select( comparison => new object[] { StringComparer.FromComparison( comparison ) } )
            .ToArray();

    public class Data : RecordTests
    {
        [Theory]
        [MemberData(nameof(StringComparerCases))]
        public void matches_source_data_and_dataflow_field_comparer( StringComparer comparer )
        {
            dataflow = dataflow with { FieldNameComparer = comparer };
            var actual = instance();
            var dictionary = actual.Data.Should().BeOfType<Dictionary<string, object?>>().Subject;
            dictionary.Comparer.Should().Be( comparer );
            dictionary.Should().BeEquivalentTo( data );
        }
    }

    public class Original : RecordTests
    {
        [Theory]
        [MemberData(nameof(StringComparerCases))]
        public void matches_source_data_and_dataflow_field_comparer( StringComparer comparer )
        {
            dataflow = dataflow with { FieldNameComparer = comparer };
            var actual = instance();
            var dictionary = actual.Original.Should().BeOfType<Dictionary<string, object?>>().Subject;
            dictionary.Comparer.Should().Be( comparer );
            dictionary.Should().BeEquivalentTo( data );
        }

        [Fact]
        public void not_same_as_data()
        {
            var actual = instance();
            actual.Original.Should().NotBeSameAs( actual.Data );
        }
    }
}
