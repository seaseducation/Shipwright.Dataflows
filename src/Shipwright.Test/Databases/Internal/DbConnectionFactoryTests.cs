// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Lamar;
using System.Data;

namespace Shipwright.Databases.Internal;

public class DbConnectionFactoryTests
{
    Mock<IServiceContext> container = new( MockBehavior.Strict );
    IDbConnectionFactory instance() => new DbConnectionFactory( container?.Object! );

    public class Constructor : DbConnectionFactoryTests
    {
        [Fact]
        public void requires_container()
        {
            container = null!;
            Assert.Throws<ArgumentNullException>( nameof(container), instance );
        }
    }

    public class Create : DbConnectionFactoryTests
    {
        DbConnectionInfo connectionInfo = new FakeDbConnectionInfo();
        IDbConnection method() => instance().Create( connectionInfo );

        [Fact]
        public void requires_connectionInfo()
        {
            connectionInfo = null!;
            Assert.Throws<ArgumentNullException>( nameof(connectionInfo), method );
        }

        [Fact]
        public void returns_connection_from_located_factory()
        {
            var expected = new Mock<IDbConnection>( MockBehavior.Strict ).Object;
            var factory = new Mock<IDbConnectionFactory<FakeDbConnectionInfo>>( MockBehavior.Strict );
            factory.Setup( _ => _.Create( (FakeDbConnectionInfo)connectionInfo ) ).Returns( expected );
            container.Setup( _ => _.GetInstance( typeof(IDbConnectionFactory<FakeDbConnectionInfo>) ) ).Returns( factory.Object );

            var actual = method();
            actual.Should().BeSameAs( expected );
        }
    }
}
