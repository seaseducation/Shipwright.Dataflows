// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Dapper;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using Shipwright.Databases;
using System.Text.RegularExpressions;

namespace Shipwright.Actions.DSCtop;

/// <summary>
/// DSCtop-specific settings.
/// </summary>
public record DsctopSettings : IActionSettings
{
    /// <summary>
    /// Oracle connection information.
    /// </summary>
    public OracleConnectionInfo ConnectionInfo { get; init; } = null!;

    /// <summary>
    /// DSCtop product name to use for customer-facing messages.
    /// </summary>
    public string ProductName { get; init; } = string.Empty;

    /// <summary>
    /// Tenant's internal district identifier.
    /// </summary>
    public long DistrictId { get; init; }

    /// <summary>
    /// Tenant's district name.
    /// </summary>
    public string DistrictName { get; init; } = string.Empty;

    /// <summary>
    /// District default parental consent value.
    /// </summary>
    public string DistrictParentalConsentDefault { get; init; } = string.Empty;

    /// <summary>
    /// District student medicaid eligible default value.
    /// </summary>
    public string DistrictEligibleDefault { get; init; } = string.Empty;

    /// <summary>
    /// District state.
    /// </summary>
    public string DistrictState { get; init; } = string.Empty;

    /// <summary>
    /// Tenant's DSCtop-specific import folder.
    /// </summary>
    public string ImportFolder { get; init; } = string.Empty;

    /// <summary>
    /// Path for the tenant's DSCtop imports.
    /// </summary>
    public string ImportPath { get; init; } = string.Empty;

    /// <summary>
    /// User identifier for imports.
    /// </summary>
    public long ImportUser { get; init; } = 99999998;

    /// <summary>
    /// Factory for generating settings.
    /// </summary>
    public class Factory : IActionSettingsFactory<DsctopSettings>
    {
        readonly IDbConnectionFactory _connectionFactory;

        public Factory( IDbConnectionFactory connectionFactory )
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException( nameof(connectionFactory) );
        }

        /// <summary>
        /// Gets the state for the DSCtop customer.
        /// Obtained from the tenant prefix when not explicitly defined in configuration.
        /// </summary>
        public virtual string GetState( string tenant, IConfiguration configuration ) =>
            configuration.GetValue( "dsctop:state", tenant switch
            {
                // texas should default to onward
                var onw when onw.StartsWith( "TX", StringComparison.OrdinalIgnoreCase ) => "ONW",
                _ => tenant[..2]
            } ).ToUpperInvariant();

        /// <summary>
        /// Gets the schema for the DSCtop customer.
        /// Obtained from the tenant state when not explicitly defined in configuration.
        /// </summary>
        public virtual string GetSchema( string tenant, IConfiguration configuration )
        {
            var state = GetState( tenant, configuration );
            return configuration.GetValue( $"dsctop:{state}:schema", $"dsctop_{state}".ToLowerInvariant() );
        }

        /// <summary>
        /// Gets the password for the DSCtop database connection.
        /// Uses the default convention when not explicitly defined in configuration.
        /// </summary>
        public virtual string GetPassword( string tenant, IConfiguration configuration )
        {
            var state = GetState( tenant, configuration );
            var schema = GetSchema( tenant, configuration );
            return configuration.GetValue( $"dsctop:{state}:password", schema );
        }

        /// <summary>
        /// Gets the connection string for the DSCtop customer database.
        /// </summary>
        public virtual OracleConnectionInfo GetConnectionInfo( string tenant, IConfiguration configuration )
        {
            var connectionString = configuration["dsctop:connectionString"];
            var builder = new OracleConnectionStringBuilder( connectionString )
            {
                UserID = GetSchema( tenant, configuration ),
                Password = GetPassword( tenant, configuration )
            };

            return new( builder.ToString() );
        }

        /// <summary>
        /// Gets the product name to use for customer-facing messages.
        /// Determined by convention if not specified in configuration.
        /// </summary>
        public virtual string GetProductName( string tenant, IConfiguration configuration ) =>
            configuration.GetValue( $"dsctop:name", tenant switch
            {
                var onward when GetState( onward, configuration ).Equals( "ONW", StringComparison.OrdinalIgnoreCase ) => "Onward",
                _ => "DSCtop"
            } );

        /// <summary>
        /// Gets district settings for the tenant from the DSCtop database.
        /// </summary>
        public virtual async Task<dynamic> GetDistrictSettings( OracleConnectionInfo connectionInfo, string tenant, CancellationToken cancellationToken )
        {
            // ReSharper disable once StringLiteralTypo
            const string sql = @"
                    SELECT
	                    DIST_ID,
	                    DIST_NAME,
	                    IMPORT_DIR,
	                    CASE
		                    WHEN DEFAULT_CONSENT = 1 THEN '1'
		                    ELSE '0'
	                    END AS DEFAULT_CONSENT,
                        CASE
                            WHEN DEFAULT_ELIGIBLE = 1 then '1'
                            ELSE '0'
                        END AS DEFAULT_ELIGIBLE
                    FROM DDATADISTRICT
                    WHERE LOWER( SSO_TENANT_NAME ) = LOWER( :tenant )";

            var command = new CommandDefinition( sql, parameters: new { tenant }, cancellationToken: cancellationToken );
            using var connection = _connectionFactory.Create( connectionInfo );
            var districts = ( await connection.QueryAsync( command ) ).ToArray();

            return districts.Length == 1
                ? districts.Single()
                : throw new InvalidOperationException( $"Found {districts.Length} districts for the tenant {tenant}" );
        }

        /// <summary>
        /// Gets the DSCtop-specific import folder.
        /// </summary>
        /// <param name="districtImportFolder">Configured import folder.</param>
        /// <param name="districtName">District name, from which to build the import folder when it is undefined.</param>
        public string GetImportFolder( string? districtImportFolder, string districtName ) => districtImportFolder switch
        {
            var folder when !string.IsNullOrWhiteSpace( folder ) => folder,
            _ => Regex.Replace( districtName, @"[^0-9a-zA-Z\s]", string.Empty )
        };

        /// <summary>
        /// Gets the path for DSCtop imports.
        /// </summary>
        public string GetImportPath( string tenant, IConfiguration configuration )
        {
            var state = GetState( tenant, configuration );
            var path = configuration["dsctop:import:path"];
            return path.Replace( "{state}", state );
        }

        /// <summary>
        /// Gets the user identifier for import audit records.
        /// Uses the standard convention if not explicitly set in configuration.
        /// </summary>
        public long GetImportUser( IConfiguration configuration ) => configuration.GetValue( "dsctop:import:user", 99999998 );

        /// <summary>
        /// Implementation of <see cref="IActionSettingsFactory{TSettings}"/>.
        /// </summary>
        public virtual async Task<DsctopSettings> Create( ActionContext context, IConfigurationRoot configuration, CancellationToken cancellationToken )
        {
            if ( context == null ) throw new ArgumentNullException( nameof(context) );
            if ( configuration == null ) throw new ArgumentNullException( nameof(configuration) );

            var tenant = context.Tenant;
            var state = GetState( tenant, configuration );
            var productName = GetProductName( tenant, configuration );
            var connectionInfo = GetConnectionInfo( tenant, configuration );
            var district = await GetDistrictSettings( connectionInfo, tenant, cancellationToken );

            return new()
            {
                ConnectionInfo = connectionInfo,
                DistrictId = district.DIST_ID,
                DistrictName = district.DIST_NAME,
                DistrictEligibleDefault = district.DEFAULT_ELIGIBLE,
                DistrictParentalConsentDefault = district.DEFAULT_CONSENT,
                DistrictState = state,
                ProductName = productName,
                ImportFolder = GetImportFolder( district.IMPORT_DIR, district.DIST_NAME ),
                ImportPath = GetImportPath( tenant, configuration ),
                ImportUser = GetImportUser( configuration ),
            };
        }
    }
}
