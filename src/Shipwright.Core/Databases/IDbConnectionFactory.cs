// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using System.Data;

namespace Shipwright.Databases;

/// <summary>
/// Defines a factory for creating arbitrary database connections.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates a database connection using the given connection information.
    /// </summary>
    /// <param name="connectionInfo">Connection information about the connection to create.</param>
    public IDbConnection Create( DbConnectionInfo connectionInfo );
}

/// <summary>
/// Defines a factory for creating database connections for a specific database type.
/// </summary>
/// <typeparam name="TConnectionInfo">Type of the connection information for which the factory creates connections.</typeparam>
public interface IDbConnectionFactory<TConnectionInfo> where TConnectionInfo : DbConnectionInfo
{
    /// <summary>
    /// Creates a database connection from the given connection information.
    /// </summary>
    /// <param name="connectionInfo">Connection information about the connection to create.</param>
    public IDbConnection Create( TConnectionInfo connectionInfo );
}
