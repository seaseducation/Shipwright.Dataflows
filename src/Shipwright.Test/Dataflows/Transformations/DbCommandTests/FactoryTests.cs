// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Databases;

namespace Shipwright.Dataflows.Transformations.DbCommandTests;

public class FactoryTests
{
    IDbConnectionFactory connectionFactory = new Mock<IDbConnectionFactory>( MockBehavior.Strict ).Object;
    ITransformationHandlerFactory<DbCommand> instance() => new DbCommand.Factory( connectionFactory );

    public class Constructor : FactoryTests
    {
        [Fact]
        public void requires_connectionFactory()
        {
            connectionFactory = null!;
            Assert.Throws<ArgumentNullException>( nameof(connectionFactory), instance );
        }
    }

    public class Create : FactoryTests
    {
        DbCommand transformation = new Fixture().WithDataflowCustomization().Create<DbCommand>();
        Task<ITransformationHandler> method() => instance().Create( transformation, default );

        [Fact]
        public async Task requires_transformation()
        {
            transformation = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(transformation), method );
        }

        [Fact]
        public async Task returns_handler()
        {
            var actual = await method();
            var handler = actual.Should().BeOfType<DbCommand.Handler>().Subject;
            handler._transformation.Should().BeSameAs( transformation );

            var helper = handler._helper.Should().BeOfType<DbCommand.Helper>().Subject;
            helper._transformation.Should().BeSameAs( transformation );
            helper._connectionFactory.Should().BeSameAs( connectionFactory );
        }
    }
}
