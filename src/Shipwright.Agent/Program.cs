// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shipwright.Actions;
using Shipwright.Actions.Internal;
using Shipwright.Commands;
using Shipwright.Commands.Internal;

var host = Host.CreateDefaultBuilder( args );

host.ConfigureAppConfiguration( ( context, configuration ) =>
{
    // add base application configuration
    configuration
        .AddYamlFile( Path.Combine( "Properties", "appsettings.yml" ) )
        .AddYamlFile( Path.Combine( "Properties", $"appsettings.{context.HostingEnvironment.EnvironmentName}.yml" ) )
        .AddYamlFile( Path.Combine( "Properties", $"appsettings.User.yml" ), true );

    // command line options should be added last to override all other configuration
    configuration.AddCommandLine( args );
} );

host.UseLamar( registry =>
{
    registry.Scan( scanner =>
    {
        scanner.AssemblyContainingType<ICommandDispatcher>();
        scanner.AssemblyContainingType<Program>();
        scanner.Exclude( type => type.Name.Contains( "Decorator" ) );
        scanner.WithDefaultConventions();
        scanner.ConnectImplementationsToTypesClosing( typeof(ICommandHandler<,>) );
        scanner.ConnectImplementationsToTypesClosing( typeof(IValidator<>) );

        // add all discovered actions by type name
        scanner.AddAllTypesOf<IActionFactory>().NameBy( type => type.Name );
    } );

    registry.For( typeof(ICommandHandler<,>) ).DecorateAllWith( typeof(CommandValidationDecorator<,>) );
    registry.For( typeof(ICommandHandler<,>) ).DecorateAllWith( typeof(CommandCancellationDecorator<,>) );
    registry.For( typeof(IActionFactory) ).DecorateAllWith( typeof(CancellationActionDecorator) );

    // register background task to run
    registry.AddHostedService<Program>();
} );

await host.RunConsoleAsync();

partial class Program : BackgroundService
{
    readonly IHostApplicationLifetime _lifetime;
    readonly IConfiguration _configuration;
    readonly ICommandDispatcher _dispatcher;

    public Program( IHostApplicationLifetime lifetime, IConfiguration configuration, ICommandDispatcher dispatcher )
    {
        _lifetime = lifetime ?? throw new ArgumentNullException( nameof(lifetime) );
        _configuration = configuration ?? throw new ArgumentNullException( nameof(configuration) );
        _dispatcher = dispatcher ?? throw new ArgumentNullException( nameof(dispatcher) );
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        try
        {
            var command = new InvokeAction
            {
                Action = _configuration["action"],
                Context = _configuration.Get<ActionContext>()
            };
            await _dispatcher.Execute( command, stoppingToken );
        }

        // always exit the console application upon service completion
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
