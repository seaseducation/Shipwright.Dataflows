// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.ReplaceTests;

public class HandlerTests
{
    Replace transformation = new Fixture().Create<Replace>();
    ITransformationHandler instance() => new Replace.Handler( transformation );

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

        public class WhenFieldNotPresent : Transform
        {
            [Fact]
            public async Task no_op()
            {
                foreach ( var field in transformation.Fields )
                    record.Data.Remove( field );

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldValueNull : Transform
        {
            [Fact]
            public async Task no_op()
            {
                foreach ( var field in transformation.Fields )
                    record.Data[field] = null;

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldValueNotMapped : Transform
        {
            [Fact]
            public async Task no_op()
            {
                var fixture = new Fixture();

                foreach ( var field in transformation.Fields )
                {
                    var value = fixture.Create<string>();

                    while ( transformation.Replacements.Select( _ => _.Incoming ).Contains( value ) )
                        value = fixture.Create<string>();

                    record[field] = value;
                }

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldValueMapped : Transform
        {
            [Fact]
            public async Task replaces_value()
            {
                var fixture = new Fixture();
                var field = fixture.Create<string>();
                var incoming = fixture.Create<string>();
                var outgoing = fixture.Create<string>();
                var expected = new Dictionary<string, object?>( record.Data );

                transformation.Fields.Add( field );
                transformation.Replacements.Add( new( incoming, outgoing ) );
                record[field] = incoming;
                expected[field] = outgoing;

                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldValueToStringMapped : Transform
        {
            [Fact]
            public async Task replaces_value()
            {
                var fixture = new Fixture();
                var field = fixture.Create<string>();
                var incoming = Guid.NewGuid();
                var outgoing = fixture.Create<string>();
                var expected = new Dictionary<string, object?>( record.Data );

                transformation.Fields.Add( field );
                transformation.Replacements.Add( new( incoming.ToString(), outgoing ) );
                record[field] = incoming;
                expected[field] = outgoing;

                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }
    }
}
