// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.DbCommandTests;

public class HandlerTests
{
    DbCommand transformation = new Fixture().WithDataflowCustomization().Create<DbCommand>();
    Mock<DbCommand.IHelper> helper = new( MockBehavior.Strict );
    ITransformationHandler instance() => new DbCommand.Handler( transformation, helper?.Object! );

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

    public class Transform : HandlerTests
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

        [Theory]
        [BooleanCases]
        public async Task executes_command( bool canceled )
        {
            cancellationToken = new( canceled );
            var fixture = new Fixture();
            var expectedParameter = new Dictionary<string, object?>();

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
            helper.Setup( _ => _.Execute( Capture.In( actualParameters ), cancellationToken ) )
                .Returns( Task.CompletedTask );

            await method();
            record.Should().BeEquivalentTo( expectedRecord );

            actualParameters.Should().ContainSingle().Subject.Should().BeEquivalentTo( expectedParameter );
        }
    }
}
