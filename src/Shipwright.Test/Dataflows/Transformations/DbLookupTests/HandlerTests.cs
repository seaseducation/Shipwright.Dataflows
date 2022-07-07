// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.DbLookupTests;

public class HandlerTests
{
    DbLookup transformation = new Fixture().WithDataflowCustomization().Create<DbLookup>();
    Mock<DbLookup.IHelper> helper = new( MockBehavior.Strict );
    ITransformationHandler instance() => new DbLookup.Handler( transformation, helper?.Object! );

    public class Constructor : HandlerTests
    {
        [Fact]
        public void requires_transformation()
        {
            transformation = null!;
            Assert.Throws<ArgumentNullException>( nameof(transformation), instance );
        }

        [Fact]
        public void requires_helper()
        {
            helper = null!;
            Assert.Throws<ArgumentNullException>( nameof(helper), instance );
        }
    }

    public abstract class Transform : HandlerTests
    {
        Record record = new Fixture().WithDataflowCustomization().Create<Record>();
        CancellationToken cancellationToken;
        Task method() => instance().Transform( record, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_record( bool canceled )
        {
            cancellationToken = new( canceled );
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        public class WhenQueryReturnsSingleMatch : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task maps_outputs_to_fields( bool canceled )
            {
                cancellationToken = new( canceled );
                var fixture = new Fixture();
                var expectedParameter = new Dictionary<string, object?>();
                var output = new Dictionary<string, object?>();

                foreach ( var ( parameter, value ) in transformation.Parameters )
                    expectedParameter[parameter] = value;

                foreach ( var ( field, parameter ) in transformation.Input )
                {
                    var value = expectedParameter[parameter ?? field] = fixture.Create<string>();
                    record[field] = value;
                }

                record = new( record.Data, record.Dataflow, record.Source, record.Position );
                var expectedRecord = new Record( record.Data, record.Dataflow, record.Source, record.Position );

                foreach ( var ( field, parameter ) in transformation.Output )
                    expectedRecord[field] = output[parameter ?? field] = fixture.Create<string>();

                var actualParameters = new List<IDictionary<string, object?>>();
                helper.Setup( _ => _.GetMatches( Capture.In( actualParameters ), cancellationToken ) )
                    .ReturnsAsync( new dynamic[] { output } );

                await method();
                record.Should().BeEquivalentTo( expectedRecord );

                actualParameters.Should().ContainSingle().Subject.Should().BeEquivalentTo( expectedParameter );
            }
        }

        public class WhenQueryDoesNotReturnSingleMatch : Transform
        {
            [Theory]
            [InlineData(0)]
            [InlineData(2)]
            [InlineData(3)]
            public async Task adds_failure_event_and_leaves_record_unchanged( int matches )
            {
                var fixture = new Fixture();
                var expectedParameter = new Dictionary<string, object?>();
                var output = fixture.CreateMany<IDictionary<string, object?>>( matches ).ToArray();

                foreach ( var ( parameter, value ) in transformation.Parameters )
                    expectedParameter[parameter] = value;

                foreach ( var ( field, parameter ) in transformation.Input )
                {
                    var value = expectedParameter[parameter ?? field] = fixture.Create<string>();
                    record[field] = value;
                }

                record = new( record.Data, record.Dataflow, record.Source, record.Position );
                var expectedRecord = new Record( record.Data, record.Dataflow, record.Source, record.Position );

                var actualParameters = new List<IDictionary<string, object?>>();
                helper.Setup( _ => _.GetMatches( Capture.In( actualParameters ), cancellationToken ) )
                    .ReturnsAsync( output );

                expectedRecord.Events.Add( transformation.OnFailure( matches, expectedParameter ) );

                await method();
                record.Should().BeEquivalentTo( expectedRecord );

                actualParameters.Should().ContainSingle().Subject.Should().BeEquivalentTo( expectedParameter );
            }
        }
    }
}
