// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Lamar;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shipwright.Actions;
using Shipwright.Commands;
using Shipwright.Commands.Internal;

var host = Host.CreateDefaultBuilder( args );

host.ConfigureAppConfiguration( ( context, configuration ) =>
{
    // command line options should be added last to override all other configuration
    configuration.AddCommandLine( args );
} );

host.UseLamar( registry =>
{
    registry.Scan( scanner =>
    {
        scanner.AssemblyContainingType<ICommandDispatcher>();
        scanner.AssemblyContainingType<Program>();
        scanner.WithDefaultConventions();
        scanner.ConnectImplementationsToTypesClosing( typeof(ICommandHandler<,>) );
        scanner.ConnectImplementationsToTypesClosing( typeof(IValidator<>) );

        // add all discovered actions by type name
        scanner.AddAllTypesOf<IActionFactory>().NameBy( type => type.Name );
    } );

    registry.For( typeof(ICommandHandler<,>) ).DecorateAllWith( typeof(CommandValidationDecorator<,>) );
    registry.For( typeof(ICommandHandler<,>) ).DecorateAllWith( typeof(CommandCancellationDecorator<,>) );

    // register background task to run
    registry.AddHostedService<Program>();
} );

await host.RunConsoleAsync();

partial class Program : BackgroundService
{
    readonly IHostApplicationLifetime _lifetime;
    readonly IConfiguration _configuration;
    readonly IServiceContext _container;

    public Program( IHostApplicationLifetime lifetime, IConfiguration configuration, IServiceContext container )
    {
        _lifetime = lifetime ?? throw new ArgumentNullException( nameof(lifetime) );
        _configuration = configuration ?? throw new ArgumentNullException( nameof(configuration) );
        _container = container ?? throw new ArgumentNullException( nameof(container) );
    }

    protected override async Task ExecuteAsync( CancellationToken stoppingToken )
    {
        try
        {
            // get execution options from configuration
            var action = _configuration["action"] ?? string.Empty;
            var context = _configuration.Get<ActionContext>();
            var factory = _container.GetInstance<IActionFactory>( action );
            var command = await factory.Create( context, stoppingToken );

            await _container.GetInstance<ICommandDispatcher>()
                .Execute( command, stoppingToken );
        }

        // always exit the console application upon service completion
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
