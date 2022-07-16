// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Commands;

/// <summary>
/// Defines a command that can be dispatched for execution, which returns a result.
/// </summary>
/// <typeparam name="TResult">Type returned when the command is executed.</typeparam>
public abstract record Command<TResult>;

/// <summary>
/// Defines a command that can be dispatched for execution, which returns no result.
/// </summary>
public abstract record Command : Command<ValueTuple>;
