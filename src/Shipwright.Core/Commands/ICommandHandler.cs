// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Core.Commands;

/// <summary>
/// Defines a handler for executing commands of a specific type.
/// </summary>
/// <typeparam name="TCommand">Type of the commands the handler can execute.</typeparam>
/// <typeparam name="TResult">Type returned when the command is executed.</typeparam>
public interface ICommandHandler<TCommand, TResult> where TCommand : Command<TResult>
{
    /// <summary>
    /// Executes the given command.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of executing the command.</returns>
    public Task<TResult> Execute( TCommand command, CancellationToken cancellationToken );
}
