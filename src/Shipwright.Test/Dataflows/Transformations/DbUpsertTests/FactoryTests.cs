// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using JetBrains.Annotations;
using Shipwright.Databases;
using SqlKata.Compilers;

namespace Shipwright.Dataflows.Transformations.DbUpsertTests;

public class FactoryTests
{
    Mock<IDbConnectionFactory> connectionFactory = new( MockBehavior.Strict );
    ITransformationHandlerFactory<DbUpsert> instance() => new DbUpsert.Factory( connectionFactory?.Object! );

    public class Constructor : FactoryTests
    {
        [Fact]
        public void requires_connectionFactory()
        {
            connectionFactory = null!;
            Assert.Throws<ArgumentNullException>( nameof(connectionFactory), instance );
        }
    }

    public abstract class Create : FactoryTests
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

        public abstract class ExpectsCompilerType<TCompiler> : Create where TCompiler : Compiler
        {
            [Theory]
            [BooleanCases]
            public async Task returns_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                var actual = await method();
                var handler = actual.Should().BeOfType<DbUpsert.Handler>().Subject;
                handler._transformation.Should().BeSameAs( transformation );
                handler._connectionFactory.Should().BeSameAs( connectionFactory.Object );
                handler._compiler.Should().BeOfType<TCompiler>();
            }
        }

        [UsedImplicitly]
        public class WhenOracle : ExpectsCompilerType<OracleCompiler>
        {
            public WhenOracle()
            {
                transformation = transformation with { ConnectionInfo = new OracleConnectionInfo( new Fixture().Create<string>() ) };
            }
        }
    }
}
