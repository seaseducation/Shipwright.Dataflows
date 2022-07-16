// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Commands;

namespace Shipwright.Actions;

/// <summary>
/// Defines a factory for creating an action.
/// This is the interface to implement for defining a custom action.
/// All instances of this type will be discoverable by their type name, which will be the name of the action.
/// </summary>
public interface IActionFactory
{
    /// <summary>
    /// Creates and configures an action to execute.
    /// </summary>
    /// <param name="context">Context of the action.</param>
    /// <param name="cancellationToken" />
    /// <returns>A command for executing the configured action.</returns>
    public Task<Command> Create( ActionContext context, CancellationToken cancellationToken );
}
