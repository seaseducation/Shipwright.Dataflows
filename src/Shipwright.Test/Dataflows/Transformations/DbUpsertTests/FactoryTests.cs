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
    Mock<ITransformationHandlerFactory> transformationHandlerFactory = new( MockBehavior.Strict );
    ITransformationHandlerFactory<DbUpsert> instance() => new DbUpsert.Factory( connectionFactory?.Object!, transformationHandlerFactory?.Object! );

    public class Constructor : FactoryTests
    {
        [Fact]
        public void requires_connectionFactory()
        {
            connectionFactory = null!;
            Assert.Throws<ArgumentNullException>( nameof(connectionFactory), instance );
        }

        [Fact]
        public void requires_transformationHandlerFactory()
        {
            transformationHandlerFactory = null!;
            Assert.Throws<ArgumentNullException>( nameof(transformationHandlerFactory), instance );
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
            public async Task returns_handler_without_optional_handlers( bool canceled )
            {
                cancellationToken = new( canceled );
                transformation.BeforeInsert.Clear();
                transformation.AfterInsert.Clear();
                transformation.BeforeUpdate.Clear();
                transformation.AfterUpdate.Clear();

                var actual = await method();
                var handler = actual.Should().BeOfType<DbUpsert.Handler>().Subject;
                handler._transformation.Should().BeSameAs( transformation );
                handler._connectionFactory.Should().BeSameAs( connectionFactory.Object );
                handler._compiler.Should().BeOfType<TCompiler>();
            }

            [Theory]
            [BooleanCases]
            public async Task returns_handler_with_optional_handlers( bool canceled )
            {
                cancellationToken = new( canceled );
                var optionals = new List<Transformation>();
                var beforeInsertHandler = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
                var afterInsertHandler = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
                var beforeUpdateHandler = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
                var afterUpdateHandler = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;

                var sequence = new MockSequence();
                transformationHandlerFactory.InSequence( sequence ).Setup( _ => _.Create( Capture.In( optionals ), cancellationToken ) ).ReturnsAsync( beforeInsertHandler );
                transformationHandlerFactory.InSequence( sequence ).Setup( _ => _.Create( Capture.In( optionals ), cancellationToken ) ).ReturnsAsync( afterInsertHandler );
                transformationHandlerFactory.InSequence( sequence ).Setup( _ => _.Create( Capture.In( optionals ), cancellationToken ) ).ReturnsAsync( beforeUpdateHandler );
                transformationHandlerFactory.InSequence( sequence ).Setup( _ => _.Create( Capture.In( optionals ), cancellationToken ) ).ReturnsAsync( afterUpdateHandler );

                var actual = await method();
                var handler = actual.Should().BeOfType<DbUpsert.Handler>().Subject;
                handler._transformation.Should().BeSameAs( transformation );
                handler._connectionFactory.Should().BeSameAs( connectionFactory.Object );
                handler._compiler.Should().BeOfType<TCompiler>();
                handler._beforeInsertHandler.Should().BeSameAs( beforeInsertHandler );
                handler._afterInsertHandler.Should().BeSameAs( afterInsertHandler );
                handler._beforeUpdateHandler.Should().BeSameAs( beforeUpdateHandler );
                handler._afterUpdateHandler.Should().BeSameAs( afterUpdateHandler );

                optionals.ElementAt( 0 ).Should().BeOfType<AggregateTransformation>().Subject.Transformations.Should().BeSameAs( transformation.BeforeInsert );
                optionals.ElementAt( 1 ).Should().BeOfType<AggregateTransformation>().Subject.Transformations.Should().BeSameAs( transformation.AfterInsert );
                optionals.ElementAt( 2 ).Should().BeOfType<AggregateTransformation>().Subject.Transformations.Should().BeSameAs( transformation.BeforeUpdate );
                optionals.ElementAt( 3 ).Should().BeOfType<AggregateTransformation>().Subject.Transformations.Should().BeSameAs( transformation.AfterUpdate );
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
