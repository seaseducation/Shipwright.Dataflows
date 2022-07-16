// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Lamar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shipwright.Commands;

namespace Shipwright.Actions.Internal;

/// <summary>
/// Command that invokes an action.
/// </summary>
public record InvokeAction : Command
{
    /// <summary>
    /// Name of the action to invoke.
    /// </summary>
    public string Action { get; init; } = string.Empty;

    /// <summary>
    /// Context that defines the tenant and configuration for the action.
    /// </summary>
    public ActionContext Context { get; init; } = new();

    /// <summary>
    /// Validator for <see cref="InvokeAction"/>.
    /// </summary>
    public class Validator : AbstractValidator<InvokeAction>
    {
        public Validator()
        {
            RuleFor( _ => _.Action ).NotEmpty();
            RuleFor( _ => _.Context ).NotNull();
        }
    }

    /// <summary>
    /// Handler for <see cref="InvokeAction"/>.
    /// </summary>
    public class Handler : CommandHandler<InvokeAction>
    {
        readonly IServiceContext _container;
        readonly IActionSettingsFactory _settingsFactory;
        readonly ILogger<InvokeAction> _logger;

        public Handler( IServiceContext container, IActionSettingsFactory settingsFactory, ILogger<InvokeAction> logger )
        {
            _container = container ?? throw new ArgumentNullException( nameof(container) );
            _settingsFactory = settingsFactory ?? throw new ArgumentNullException( nameof(settingsFactory) );
            _logger = logger ?? throw new ArgumentNullException( nameof(logger) );
        }

        /// <summary>
        /// Gets the collection of <see cref="ActionContext"/> for which the action should be executed.
        /// </summary>
        /// <param name="command">Command for which to return tenant action contexts.</param>
        /// <returns>An <see cref="ActionContext"/> collection containing all the tenants for which the action should be
        /// executed.</returns>
        IEnumerable<ActionContext> GetContexts( InvokeAction command )
        {
            // return only the given context when the tenant is specified
            if ( !string.IsNullOrWhiteSpace( command.Context.Tenant ) )
            {
                yield return command.Context;
                yield break;
            }

            var tenants = _settingsFactory.For( command.Action, command.Context )
                .GetSection( "tenants" )
                .GetChildren()
                .ToArray();

            if ( tenants.Length == 0 )
                throw new InvalidOperationException( $"No tenants are defined for the action {command.Action}" );

            foreach ( var tenant in tenants )
            {
                // if no specific configurations are required, default to the tenant
                var configs = tenant.Get<string[]>() ?? new[] { string.Empty };

                foreach ( var config in configs )
                    yield return new ActionContext { Tenant = tenant.Key, Configuration = config };
            }
        }

        protected override async Task ExecuteCommand( InvokeAction command, CancellationToken cancellationToken )
        {
            if ( command == null ) throw new ArgumentNullException( nameof(command) );

            var factory = _container.GetInstance<IActionFactory>( command.Action );
            var dispatcher = _container.GetInstance<ICommandDispatcher>();
            var contexts = GetContexts( command );

            foreach ( var context in contexts )
            {
                var action = await factory.Create( context, cancellationToken );

                _logger.LogInformation( "Starting action {Action} for tenant {Tenant}", command.Action, context.Tenant );
                await dispatcher.Execute( action, cancellationToken );
                _logger.LogInformation( "Completed action {Action} for tenant {Tenant}", command.Action, context.Tenant );
            }
        }
    }
}
