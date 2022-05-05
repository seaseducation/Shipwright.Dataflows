// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Core.Commands;

/// <summary>
/// Defines a command that can be executed.
/// </summary>
/// <typeparam name="TResult">Type returned when the command is executed.</typeparam>
[PublicAPI]
public abstract record Command<TResult> {}
