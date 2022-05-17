// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Lamar.Microsoft.DependencyInjection;
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
} );

await host.RunConsoleAsync();
