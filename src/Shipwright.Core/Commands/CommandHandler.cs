// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Commands;

/// <summary>
/// Defines a handler that can execute commands of a specific type that return a result.
/// This base type will perform argument checking boilerplate.
/// </summary>
/// <typeparam name="TCommand">Type of command the handler can execute.</typeparam>
/// <typeparam name="TResult">Type returned when a command is executed.</typeparam>
public abstract class CommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
    where TCommand : Command<TResult>
{
    /// <summary>
    /// Executes the given command.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken" />
    /// <returns>The result of executing the command.</returns>
    protected abstract Task<TResult> ExecuteCommand( TCommand command, CancellationToken cancellationToken );

    /// <summary>
    /// Implementation of <see cref="ICommandHandler{TCommand,TResult}" />.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken" />
    /// <returns>The result of executing the command.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="command" /> is null.</exception>
    public Task<TResult> Execute( TCommand command, CancellationToken cancellationToken )
    {
        if ( command == null ) throw new ArgumentNullException( nameof(command) );
        return ExecuteCommand( command, cancellationToken );
    }
}

/// <summary>
/// Defines a handler that can execute commands of a specific type that return no result.
/// This base type will perform argument checking boilerplate.
/// </summary>
/// <typeparam name="TCommand">Type of command the handler can execute.</typeparam>
public abstract class CommandHandler<TCommand> : ICommandHandler<TCommand> where TCommand : Command
{
    /// <summary>
    /// Executes the given command.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken" />
    protected abstract Task ExecuteCommand( TCommand command, CancellationToken cancellationToken );

    /// <summary>
    /// Implementation of <see cref="ICommandHandler{TCommand}" />.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken" />
    /// <exception cref="ArgumentNullException"><paramref name="command" /> is null.</exception>
    /// <returns>A valueless <see cref="ValueTuple"/>.</returns>
    public async Task<ValueTuple> Execute( TCommand command, CancellationToken cancellationToken )
    {
        if ( command == null ) throw new ArgumentNullException( nameof(command) );
        await ExecuteCommand( command, cancellationToken );
        return default;
    }
}
