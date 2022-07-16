// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Databases;

namespace Shipwright.Dataflows.Transformations.DbLookupTests;

public class FactoryTests
{
    IDbConnectionFactory connectionFactory = new Mock<IDbConnectionFactory>( MockBehavior.Strict ).Object;
    ITransformationHandlerFactory<DbLookup> instance() => new DbLookup.Factory( connectionFactory );

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
        DbLookup transformation = new Fixture().WithDataflowCustomization().Create<DbLookup>();
        Task<ITransformationHandler> method() => instance().Create( transformation, default );

        [Fact]
        public async Task requires_transformation()
        {
            transformation = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(transformation), method );
        }

        public class WhenCachingDisabled : Create
        {
            [Fact]
            public async Task returns_handler_with_stock_helper()
            {
                transformation = transformation with { CacheResults = false };

                var actual = await method();
                var handler = actual.Should().BeOfType<DbLookup.Handler>().Subject;
                handler._transformation.Should().BeSameAs( transformation );

                var helper = handler._helper.Should().BeOfType<DbLookup.Helper>().Subject;
                helper._transformation.Should().BeSameAs( transformation );
                helper._connectionFactory.Should().BeSameAs( connectionFactory );
            }
        }

        public class WhenCachingEnabled : Create
        {
            [Fact]
            public async Task returns_handler_with_cache_decorated_helper()
            {
                transformation = transformation with { CacheResults = true };

                var actual = await method();
                var handler = actual.Should().BeOfType<DbLookup.Handler>().Subject;
                handler._transformation.Should().BeSameAs( transformation );

                var decorator = handler._helper.Should().BeOfType<DbLookup.CacheHelperDecorator>().Subject;
                var helper = decorator._inner.Should().BeOfType<DbLookup.Helper>().Subject;
                helper._transformation.Should().BeSameAs( transformation );
                helper._connectionFactory.Should().BeSameAs( connectionFactory );
            }
        }
    }
}
