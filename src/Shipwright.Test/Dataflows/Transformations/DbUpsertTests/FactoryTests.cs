// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.DbUpsertTests;

public class FactoryTests
{
    ITransformationHandlerFactory<DbUpsert> instance() => new DbUpsert.Factory();

    public class Constructor : FactoryTests
    {

    }

    public class Create : FactoryTests
    {
        DbUpsert transformation = new Fixture().WithDataflowCustomization().Create<DbUpsert>();
        CancellationToken cancellationToken;
        Task<ITransformationHandler> method() => instance().Create( transformation, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_transformation( bool canceled )
        {
            cancellationToken = new( canceled );
            transformation = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(transformation), method );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_handler( bool canceled )
        {
            cancellationToken = new( canceled );
            var actual = await method();
            actual.Should().BeOfType<DbUpsert.Handler>();
        }
    }
}
