// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Lamar;
using Microsoft.Extensions.Configuration;

namespace Shipwright.Actions.Internal;

/// <summary>
/// Implementation of <see cref="IActionSettingsFactory" />.
/// </summary>
public class ActionSettingsFactory : IActionSettingsFactory
{
    readonly IConfiguration _configuration;
    readonly IServiceContext _container;

    public ActionSettingsFactory( IConfiguration configuration, IServiceContext container )
    {
        _configuration = configuration ?? throw new ArgumentNullException( nameof(configuration) );
        _container = container ?? throw new ArgumentNullException( nameof(container) );
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
            // locate config folder - it may be nested in a subfolder
            var matching = Directory.GetDirectories( root, config, SearchOption.AllDirectories )
                .Select( folder => Path.Combine( folder, $"{action}.yml" ) )
                .Where( File.Exists )
                .ToArray();

            // build path for the configuration file
            var path = matching.Length switch
            {
                0 => Path.Combine( root, config, $"{action}.yml" ),
                1 => matching.Single(),
                _ => throw new InvalidOperationException( $"Found {matching.Length} conflicting configuration files for {config}/{action}" ),
            };

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

    public Task<TSettings> Create<TSettings>( ActionContext context, IConfigurationRoot configuration, CancellationToken cancellationToken ) where TSettings : IActionSettings
    {
        if ( context == null ) throw new ArgumentNullException( nameof(context) );
        if ( configuration == null ) throw new ArgumentNullException( nameof(configuration) );

        var settingsType = typeof(TSettings);
        var factoryType = typeof(IActionSettingsFactory<>).MakeGenericType( settingsType );
        dynamic factory = _container.GetInstance( factoryType );

        return factory.Create( context, configuration, cancellationToken );
    }
}
