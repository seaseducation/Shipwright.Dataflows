// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Microsoft.Extensions.Configuration;

namespace Shipwright.Actions.Internal;

/// <summary>
/// Implementation of <see cref="IActionSettings" />.
/// </summary>
public class ActionSettings : IActionSettings
{
    readonly IConfiguration _configuration;

    public ActionSettings( IConfiguration configuration )
    {
        _configuration = configuration ?? throw new ArgumentNullException( nameof(configuration) );
    }

    public IConfigurationRoot For( string action, ActionContext context )
    {
        if ( action == null ) throw new ArgumentNullException( nameof(action) );
        if ( context == null ) throw new ArgumentNullException( nameof(context) );

        var root = _configuration["configurationPath"] ?? "Configurations";
        var paths = new List<string>(); // use stack to enumerate in reverse order

        // top-level configuration is defined in the context, or else defaults to the tenant
        // if both are empty, only the defaults will be loaded - this will be the case when executing a tenant list
        var config = !string.IsNullOrWhiteSpace( context.Configuration ) ? context.Configuration : context.Tenant;

        // add parent configuration hierarchy to path list
        while ( !string.IsNullOrWhiteSpace( config ) )
        {
            // build path for the configuration file
            var path = Path.Combine( root, config, $"{action}.yml" );

            if ( paths.Contains( path ) )
                throw new InvalidOperationException( "Circular parent hierarchy in action configuration" );

            paths.Add( path );
            config = new ConfigurationBuilder().AddYamlFile( path, true ).Build()["parent"];
        }

        // app configuration will contain global and environment defaults
        var builder = new ConfigurationBuilder().AddConfiguration( _configuration );

        // add tenant defaults when a tenant is specified
        if ( !string.IsNullOrWhiteSpace( context.Tenant ) )
            builder.AddYamlFile( Path.Combine( root, context.Tenant, "_Default.yml" ), true );

        // add action defaults
        builder.AddYamlFile( Path.Combine( root, $"{action}.yml" ), true );

        // add paths in reverse order so they apply from least-to-most specific
        paths.Reverse();
        foreach ( var path in paths )
            builder.AddYamlFile( path, true );

        return builder.Build();
    }
}