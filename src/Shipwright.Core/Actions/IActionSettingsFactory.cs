// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Microsoft.Extensions.Configuration;

namespace Shipwright.Actions;

/// <summary>
/// Defines a factory for obtaining configuration information for actions.
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

    /// <summary>
    /// Creates typed configuration settings for an action.
    /// </summary>
    /// <param name="context">Context that defines the tenant for which the action will execute.</param>
    /// <param name="configuration">Configuration for the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TSettings">Type of the settings object to create.</typeparam>
    public Task<TSettings> Create<TSettings>( ActionContext context, IConfigurationRoot configuration, CancellationToken cancellationToken )
        where TSettings : IActionSettings;
}

/// <summary>
/// Defines a factory for obtaining action-specific settings of a specific type.
/// </summary>
/// <typeparam name="TSettings">Type for which action settings are created.</typeparam>
public interface IActionSettingsFactory<TSettings> where TSettings : IActionSettings
{
    /// <summary>
    /// Creates typed settings for an action.
    /// </summary>
    /// <param name="context">Context that defines the tenant for which the action will execute.</param>
    /// <param name="configuration">Configuration for the action.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<TSettings> Create( ActionContext context, IConfigurationRoot configuration, CancellationToken cancellationToken );
}
