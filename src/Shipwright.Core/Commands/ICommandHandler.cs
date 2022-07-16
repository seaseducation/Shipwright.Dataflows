// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Commands;

/// <summary>
/// Defines a handler that can execute commands of a specific type.
/// </summary>
/// <typeparam name="TCommand">Type of command the handler can execute.</typeparam>
/// <typeparam name="TResult">Type returned when a command is executed.</typeparam>
// ReSharper disable once TypeParameterCanBeVariant
public interface ICommandHandler<TCommand,TResult> where TCommand : Command<TResult>
{
    /// <summary>
    /// Executes the given command.
    /// </summary>
    /// <param name="command">Command to execute.</param>
    /// <param name="cancellationToken"/>
    /// <returns>The result of executing the command.</returns>
    public Task<TResult> Execute( TCommand command, CancellationToken cancellationToken );
}

/// <summary>
/// Defines a handler that can execute commands of a specific type that return no result.
/// </summary>
/// <typeparam name="TCommand">Type of command the handler can execute.</typeparam>
public interface ICommandHandler<TCommand> : ICommandHandler<TCommand,ValueTuple> where TCommand : Command {}
