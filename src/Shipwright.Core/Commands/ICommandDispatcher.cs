// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Commands;

/// <summary>
/// Defines a dispatcher for locating and executing command handlers for any command type.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Locates and executes the handler for the given command.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken" />
    /// <typeparam name="TResult">Type returned when a command is executed.</typeparam>
    /// <returns>The result of executing the command.</returns>
    public Task<TResult> Execute<TResult>( Command<TResult> command, CancellationToken cancellationToken );
}
