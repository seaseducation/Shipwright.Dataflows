// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Commands.Internal;

/// <summary>
/// Decorates a command handler to add pre-execute validation support.
/// </summary>
/// <typeparam name="TCommand">Type of the command whose handler to decorate.</typeparam>
/// <typeparam name="TResult">Type returned when the command is executed.</typeparam>
public class CommandValidationDecorator<TCommand, TResult> : ICommandHandler<TCommand, TResult> where TCommand : Command<TResult>
{
    readonly ICommandHandler<TCommand, TResult> _inner;
    readonly IValidator<TCommand> _validator;

    public CommandValidationDecorator( ICommandHandler<TCommand, TResult> inner, IValidator<TCommand> validator )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
        _validator = validator ?? throw new ArgumentNullException( nameof(validator) );
    }

    public async Task<TResult> Execute( TCommand command, CancellationToken cancellationToken )
    {
        if ( command == null ) throw new ArgumentNullException( nameof(command) );

        var result = await _validator.ValidateAsync( command, cancellationToken );

        if ( !result.IsValid )
            throw new ValidationException( $"Validation failed for command type {typeof(TCommand)}", result.Errors );

        return await _inner.Execute( command, cancellationToken );
    }
}
