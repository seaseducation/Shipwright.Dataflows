// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Lamar;
using System.Data;

namespace Shipwright.Databases.Internal;

/// <summary>
/// Implementation of <see cref="IDbConnectionFactory"/> using Lamar.
/// </summary>
public class DbConnectionFactory : IDbConnectionFactory
{
    readonly IServiceContext _container;

    public DbConnectionFactory( IServiceContext container )
    {
        _container = container ?? throw new ArgumentNullException( nameof(container) );
    }

    public IDbConnection Create( DbConnectionInfo connectionInfo )
    {
        if ( connectionInfo == null ) throw new ArgumentNullException( nameof(connectionInfo) );

        var connectionInfoType = connectionInfo.GetType();
        var factoryType = typeof(IDbConnectionFactory<>).MakeGenericType( connectionInfoType );
        dynamic factory = _container.GetInstance( factoryType );

        return factory.Create( (dynamic)connectionInfo );
    }
}
