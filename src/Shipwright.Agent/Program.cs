// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shipwright.Commands;

var host = Host.CreateDefaultBuilder( args );

host.UseLamar( registry =>
{
    registry.Scan( scanner =>
    {
        scanner.AssemblyContainingType<ICommandDispatcher>();
        scanner.AssemblyContainingType<Program>();
        scanner.WithDefaultConventions();
        scanner.ConnectImplementationsToTypesClosing( typeof(ICommandHandler<,>) );
    } );
} );

await host.RunConsoleAsync();
