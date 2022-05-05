// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Core.Commands;

/// <summary>
/// Defines a dispatcher for executing commands of any type.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>
    /// Locates and executes the handler for the given command.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TResult">Type returned by the command handler.</typeparam>
    /// <returns>The result of executing the command.</returns>
    public Task<TResult> Execute<TResult>( Command<TResult> command, CancellationToken cancellationToken );
}
