// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Commands;

namespace Shipwright.Databases;

/// <summary>
/// Defines connection information for connecting to a database.
/// </summary>
public abstract record DbConnectionInfo : Command<IDbConnectionFactory>;
