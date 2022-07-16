// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Microsoft.Extensions.Configuration;

namespace Shipwright.Actions;

/// <summary>
/// Factory for obtaining configuration information for actions.
/// </summary>
public interface IActionSettingsFactory
{
    /// <summary>
    /// Obtains configuration for the specified action.
    /// </summary>
    /// <param name="action">Action whose configuration to build.</param>
    /// <param name="context">Context that defines the tenant for which the action will execute.</param>
    public IConfigurationRoot For( string action, ActionContext context );

    /// <summary>
    /// Obtains configuration for the specified action.
    /// </summary>
    /// <param name="context">Context that defines the tenant for which the action will execute.</param>
    /// <typeparam name="TAction">Type of the action whose configuration to build.</typeparam>
    public IConfigurationRoot For<TAction>( ActionContext context ) =>
        For( typeof(TAction).Name, context );
}
