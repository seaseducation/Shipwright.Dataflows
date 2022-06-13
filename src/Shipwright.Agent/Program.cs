// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using FluentValidation;
using Lamar.Microsoft.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shipwright.Actions;
using Shipwright.Actions.Internal;
using Shipwright.Commands;
using Shipwright.Commands.Internal;
using Shipwright.Dataflows.EventSinks;
using Shipwright.Dataflows.EventSinks.Internal;
using Shipwright.Dataflows.Sources;
using Shipwright.Dataflows.Sources.Internal;
using Shipwright.Dataflows.Transformations;
using Shipwright.Dataflows.Transformations.Internal;

var host = Host.CreateDefaultBuilder( args );

host.ConfigureAppConfiguration( ( context, configuration ) =>
{
    // add base application configuration
    configuration
        .AddYamlFile( Path.Combine( "Properties", "appsettings.yml" ) )
        .AddYamlFile( Path.Combine( "Properties", $"appsettings.{context.HostingEnvironment.EnvironmentName}.yml" ) )
        .AddYamlFile( Path.Combine( "Properties", $"appsettings.User.yml" ), true );

    // add aws parameter store secrets
    // get credentials from the defined profile if present, otherwise run as the EC2 instance
    var aws = configuration.Build().GetAWSOptions( "aws:parameterStore" );
    var chain = new CredentialProfileStoreChain( aws.ProfilesLocation );
    aws.Credentials = !string.IsNullOrWhiteSpace( aws.Profile ) && chain.TryGetAWSCredentials( aws.Profile, out var credentials )
        ? credentials
        : new InstanceProfileAWSCredentials();

    configuration
        .AddSystemsManager( "/shipwright.v6/all" )
        .AddSystemsManager( $"/shipwright.v5/{context.HostingEnvironment.EnvironmentName}" )
        // command line options should be added last to override all other configuration
        .AddCommandLine( args );
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
        scanner.ConnectImplementationsToTypesClosing( typeof(ISourceReaderFactory<>) );
        scanner.ConnectImplementationsToTypesClosing( typeof(ITransformationHandlerFactory<>) );
        scanner.ConnectImplementationsToTypesClosing( typeof(IEventSinkHandlerFactory<>) );

        // add all discovered actions by type name
        scanner.AddAllTypesOf<IActionFactory>().NameBy( type => type.Name );
    } );

    registry.For( typeof(ICommandHandler<,>) ).DecorateAllWith( typeof(CommandValidationDecorator<,>) );
    registry.For( typeof(ICommandHandler<,>) ).DecorateAllWith( typeof(CommandCancellationDecorator<,>) );
    registry.For( typeof(IActionFactory) ).DecorateAllWith( typeof(CancellationActionDecorator) );
    registry.For( typeof(ISourceReaderFactory<>) ).DecorateAllWith( typeof(SourceReaderFactoryValidationDecorator<>) );
    registry.For( typeof(ISourceReaderFactory<>) ).DecorateAllWith( typeof(SourceReaderFactoryCancellationDecorator<>) );
    registry.For( typeof(ITransformationHandlerFactory<>) ).DecorateAllWith( typeof(TransformationHandlerFactoryValidationDecorator<>) );
    registry.For( typeof(ITransformationHandlerFactory<>) ).DecorateAllWith( typeof(TransformationHandlerFactoryEventDecorator<>) );
    registry.For( typeof(ITransformationHandlerFactory<>) ).DecorateAllWith( typeof(TransformationHandlerFactoryCancellationDecorator<>) );
    registry.For( typeof(IEventSinkHandlerFactory<>) ).DecorateAllWith( typeof(EventSinkHandlerFactoryValidationDecorator<>) );

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
