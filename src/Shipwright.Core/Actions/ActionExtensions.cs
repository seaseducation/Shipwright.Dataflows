// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Shipwright.Dataflows.Sources;
using System.Globalization;

namespace Shipwright.Actions;

/// <summary>
/// Productivity extensions for building actions.
/// </summary>
public static class ActionExtensions
{
    /// <summary>
    /// Obtains a collection of CSV data sources based on the source in the given configuration.
    /// </summary>
    /// <param name="configuration">Action configuration.</param>
    /// <param name="tenant">Tenant for which the sources are located.</param>
    public static IEnumerable<CsvSource> GetCsvSources( this IConfiguration configuration, string tenant )
    {
        // get csv settings from config
        var csvConfig = new CsvConfiguration( CultureInfo.InvariantCulture );
        configuration.GetSection( "source" ).Bind( csvConfig );

        var path = configuration["source:path"]
            .Replace( "{tenantImportPath}", configuration["tenant:import:path"] )
            .Replace( "{tenant}", tenant );

        var file = configuration["source:file"];
        var skip = configuration.GetValue( "source:skip", 0 );

        // get all file names matching the spec
        var matches = Directory.GetFiles( path, file, SearchOption.TopDirectoryOnly );

        CsvSource create( string filePath, string description ) => new()
        {
            Configuration = csvConfig,
            Description = description,
            Path = filePath,
            Skip = skip
        };

        foreach ( var match in matches )
            yield return create( match, Path.GetFileName( match ) );

        // when there are no matches, use the base settings
        // this will yield a file not found event
        if ( !matches.Any() )
            yield return create( Path.Combine( path, file ), file );
    }

    /// <summary>
    /// Flattens the given configuration and removes blank values.
    /// </summary>
    /// <param name="configuration">Configuration to flatten.</param>
    public static IConfigurationRoot Flatten( this IConfigurationRoot configuration )
    {
        var map = new ConfigurationBuilder().AddInMemoryCollection().Build();

        void extract( IConfiguration item )
        {
            foreach ( var child in item.GetChildren() )
            {
                if ( !string.IsNullOrWhiteSpace( child.Value ) )
                    map[child.Path] = child.Value;

                else
                    extract( child );
            }
        }

        extract( configuration );
        return map;
    }
}
