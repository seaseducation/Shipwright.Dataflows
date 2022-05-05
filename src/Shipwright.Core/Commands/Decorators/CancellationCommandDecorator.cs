// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Core.Commands.Decorators;

/// <summary>
/// Decorates a command handler to add pre-execute cancellation.
/// </summary>
/// <typeparam name="TCommand">Type of the command whose handler to decorate.</typeparam>
/// <typeparam name="TResult">Type returned when the command is executed.</typeparam>
public class CancellationCommandDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult> where TCommand : Command<TResult>
{
    readonly ICommandHandler<TCommand, TResult> _inner;

    public CancellationCommandDecorator( ICommandHandler<TCommand, TResult> inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public async Task<TResult> Execute( TCommand command, CancellationToken cancellationToken )
    {
        if ( command == null ) throw new ArgumentNullException( nameof(command) );

        cancellationToken.ThrowIfCancellationRequested();
        return await _inner.Execute( command, cancellationToken );
    }
}
