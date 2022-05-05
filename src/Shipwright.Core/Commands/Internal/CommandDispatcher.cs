// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Shipwright.Core.Commands.Internal;

/// <summary>
/// Implementation of <see cref="ICommandDispatcher" /> that uses dependency injection as a service locator.
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    readonly IServiceProvider _serviceProvider;

    public CommandDispatcher( IServiceProvider serviceProvider )
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException( nameof(serviceProvider) );
    }

    public async Task<TResult> Execute<TResult>( Command<TResult> command, CancellationToken cancellationToken )
    {
        if ( command == null ) throw new ArgumentNullException( nameof(command) );

        var commandType = command.GetType();
        var resultType = typeof(TResult);
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType( commandType, resultType );

        dynamic handler = _serviceProvider.GetRequiredService( handlerType );

        // use of the dynamic type offloads the complex reflection, expression tree caching,
        // and delegate compilation to the DLR. this results in reflection overhead only applying
        // to the first call; subsequent calls perform similar to statically-compiled statements.
        return await handler.Execute( (dynamic)command, cancellationToken );
    }
}
