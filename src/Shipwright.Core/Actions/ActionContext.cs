// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Actions;

/// <summary>
/// Metadata for an executing action.
/// </summary>
public record ActionContext
{
    /// <summary>
    /// Tenant for which the action is being executed.
    /// </summary>
    public string Tenant { get; init; } = string.Empty;

    /// <summary>
    /// Configuration to use.
    /// Defaults to the tenant configuration when <see cref="Tenant"/> is unspecified.
    /// </summary>
    public string? Configuration { get; init; } = null;
}
