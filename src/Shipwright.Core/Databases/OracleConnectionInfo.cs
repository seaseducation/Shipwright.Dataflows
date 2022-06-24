// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace Shipwright.Databases;

/// <summary>
/// Oracle database connection information.
/// </summary>
/// <param name="ConnectionString">Base connection string for the database.</param>
public record OracleConnectionInfo( string ConnectionString ) : DbConnectionInfo
{
    public OracleConnectionInfo WithUser( string userId ) => new(
        new OracleConnectionStringBuilder( ConnectionString ) { UserID = userId }.ToString() );

    public OracleConnectionInfo WithPassword( string password ) => new(
        new OracleConnectionStringBuilder( ConnectionString ) { Password = password }.ToString() );

    public class Factory : IDbConnectionFactory<OracleConnectionInfo>
    {
        public IDbConnection Create( OracleConnectionInfo connectionInfo )
        {
            if ( connectionInfo == null ) throw new ArgumentNullException( nameof(connectionInfo) );
            return new OracleConnection( connectionInfo.ConnectionString );
        }
    }
}
