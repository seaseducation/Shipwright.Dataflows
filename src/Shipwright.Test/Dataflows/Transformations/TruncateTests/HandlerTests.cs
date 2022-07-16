// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.TruncateTests;

public class HandlerTests
{
    Truncate transformation = new Fixture().Create<Truncate>();
    ITransformationHandler instance() => new Truncate.Handler( transformation );

    public class Constructor : HandlerTests
    {
        [Fact]
        public void requires_transformation()
        {
            transformation = null!;
            Assert.Throws<ArgumentNullException>( nameof(transformation), instance );
        }
    }

    public abstract class Transform : HandlerTests
    {
        Record record = new Fixture().WithDataflowCustomization().Create<Record>();
        Task method() => instance().Transform( record, default );

        [Fact]
        public async Task requires_record()
        {
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        public class WhenFieldMissing : Transform
        {
            [Fact]
            public async Task no_op()
            {
                foreach ( var (field, _) in transformation.Fields )
                    record.Data.Remove( field );

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldNonString : Transform
        {
            [Fact]
            public async Task no_op()
            {
                foreach ( var (field, _) in transformation.Fields )
                    record[field] = Guid.NewGuid();

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldStringLessThanOrEqualToLength : Transform
        {
            [Theory]
            [InlineData(0)]
            [InlineData(1)]
            public async Task no_op( int buffer )
            {
                var fixture = new Fixture();
                transformation.Fields.Clear();
                for ( var i = 0; i < 3; i++ )
                {
                    var field = fixture.Create<string>();
                    var value = fixture.Create<string>();
                    transformation.Fields.Add( new( field, value.Length + buffer ) );
                    record.Data[field] = value;
                }

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldStringGreaterThanLength : Transform
        {
            [Theory]
            [InlineData(1)]
            [InlineData(2)]
            public async Task truncates_to_set_length( int excess )
            {
                var fixture = new Fixture();
                transformation.Fields.Clear();
                var expected = new Dictionary<string, object?>( record.Data );

                for ( var i = 0; i < 3; i++ )
                {
                    var field = fixture.Create<string>();
                    var value = fixture.Create<string>();
                    transformation.Fields.Add( new( field, value.Length - excess ) );
                    record.Data[field] = value;
                    expected[field] = value.Remove( value.Length - excess );
                }

                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }
    }
}
