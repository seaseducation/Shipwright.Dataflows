// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shipwright.Actions;
using Shipwright.Commands;
using Shipwright.Commands.Internal;

var host = Host.CreateDefaultBuilder( args );

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

    public Program( IHostApplicationLifetime lifetime )
    {
        _lifetime = lifetime ?? throw new ArgumentNullException( nameof(lifetime) );
    }

    protected override Task ExecuteAsync( CancellationToken stoppingToken )
    {
        try
        {
            // todo: launch action
            throw new NotImplementedException();
        }

        // always exit the console application upon service completion
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
