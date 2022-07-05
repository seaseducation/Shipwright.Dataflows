// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.UniqueTests;

public class HandlerTests
{
    Unique transformation = new Fixture().Create<Unique>();
    ITransformationHandler instance() => new Unique.Handler( transformation );

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
        readonly Fixture _fixture = new Fixture().WithDataflowCustomization();
        readonly List<Record> records = new();
        async Task method()
        {
            var handler = instance();

            foreach ( var record in records )
                await handler.Transform( record, default );
        }

        protected Transform()
        {
            var fields = _fixture.CreateMany<string>().ToList();
            transformation = transformation with { Fields = fields };

            for ( var i = 0; i < 3; i++ )
            {
                var record = _fixture.Create<Record>();
                record.Events.Clear();

                foreach ( var field in fields )
                    record[field] = Guid.NewGuid();

                records.Add( new( record.Data, record.Dataflow, record.Source, record.Position ) );
            }
        }

        [Fact]
        public async Task requires_record()
        {
            Record record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), () => instance().Transform( record, default ) );
        }

        public class WhenKeyValuesUnique : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task no_op( bool caseSensitive )
            {
                transformation = transformation with { CaseSensitive = caseSensitive };
                var expected = records.Select( record => new Record( record.Data, record.Dataflow, record.Source, record.Position ) ).ToList();
                await method();
                records.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenKeyValuesDuplicate : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task adds_event_to_duplicate_record( bool caseSensitive )
            {
                transformation = transformation with { CaseSensitive = caseSensitive };
                var expected = records.Select( record => new Record( record.Data, record.Dataflow, record.Source, record.Position ) ).ToList();

                foreach ( var record in records.ToArray() )
                {
                    var duplicate = _fixture.Create<Record>();

                    foreach ( var field in transformation.Fields )
                        duplicate[field] = record[field]!.ToString();

                    var @event = transformation.OnFailure( record.Source, record.Position );
                    records.Add( new( duplicate.Data, duplicate.Dataflow, duplicate.Source, duplicate.Position ) );
                    expected.Add( new( duplicate.Data, duplicate.Dataflow, duplicate.Source, duplicate.Position ) { Events = { @event } } );
                }

                await method();
                records.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenCaseSensitive : Transform
        {
            [Fact]
            public async Task key_values_differing_by_case_treated_as_unique()
            {
                transformation = transformation with { CaseSensitive = true };
                var expected = records.Select( record => new Record( record.Data, record.Dataflow, record.Source, record.Position ) ).ToList();

                foreach ( var record in records.ToArray() )
                {
                    var duplicate = _fixture.Create<Record>();

                    foreach ( var field in transformation.Fields )
                        duplicate[field] = record[field]!.ToString()!.ToUpperInvariant();

                    records.Add( new( duplicate.Data, duplicate.Dataflow, duplicate.Source, duplicate.Position ) );
                    expected.Add( new( duplicate.Data, duplicate.Dataflow, duplicate.Source, duplicate.Position ) );
                }

                await method();
                records.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenCaseInsensitive : Transform
        {
            [Fact]
            public async Task key_values_differing_by_case_treated_as_duplicate()
            {
                transformation = transformation with { CaseSensitive = false };
                var expected = records.Select( record => new Record( record.Data, record.Dataflow, record.Source, record.Position ) ).ToList();

                foreach ( var record in records.ToArray() )
                {
                    var duplicate = _fixture.Create<Record>();

                    foreach ( var field in transformation.Fields )
                        duplicate[field] = record[field]!.ToString()!.ToUpperInvariant();

                    var @event = transformation.OnFailure( record.Source, record.Position );
                    records.Add( new( duplicate.Data, duplicate.Dataflow, duplicate.Source, duplicate.Position ) );
                    expected.Add( new( duplicate.Data, duplicate.Dataflow, duplicate.Source, duplicate.Position ) { Events = { @event } } );
                }

                await method();
                records.Should().BeEquivalentTo( expected );
            }
        }
    }
}
