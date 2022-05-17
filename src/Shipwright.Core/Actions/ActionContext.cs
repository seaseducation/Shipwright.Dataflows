// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Actions;

/// <summary>
/// Metadata for an executing action.
/// </summary>
/// <param name="Tenant">Tenant for which the action is being executed.</param>
/// <param name="Configuration">Alternate configuration to use (if different than the tenant).</param>
public record ActionContext( string Tenant, string? Configuration = null );
