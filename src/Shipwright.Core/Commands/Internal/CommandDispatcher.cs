// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Lamar;

namespace Shipwright.Commands.Internal;

/// <summary>
/// Implementation of <see cref="ICommandDispatcher" /> using Lamar.
/// </summary>
public class CommandDispatcher : ICommandDispatcher
{
    readonly IServiceContext _container;

    public CommandDispatcher( IServiceContext container )
    {
        _container = container ?? throw new ArgumentNullException( nameof(container) );
    }

    public async Task<TResult> Execute<TResult>( Command<TResult> command, CancellationToken cancellationToken )
    {
        if ( command == null ) throw new ArgumentNullException( nameof(command) );
        cancellationToken.ThrowIfCancellationRequested();

        var commandType = command.GetType();
        var resultType = typeof(TResult);
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType( commandType, resultType );

        dynamic handler = _container.GetInstance( handlerType );

        // note: offloading to the DLR only incurs reflection penalties the first execution
        return await handler.Execute( (dynamic)command, cancellationToken );
    }
}
