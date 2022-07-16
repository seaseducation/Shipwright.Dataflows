// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Microsoft.Extensions.Logging;

namespace Shipwright.Dataflows;

/// <summary>
/// Event that occurs during dataflow processing.
/// </summary>
/// <param name="StopProcessing">Whether the event should stop record or dataflow processing.</param>
/// <param name="Level"><see cref="LogLevel"/> of the event that determines whether the event is logged.</param>
/// <param name="Description">Description of the event.</param>
/// <param name="Value">Value or values that provide context to the event. Should be serializable.</param>
public record LogEvent( bool StopProcessing, LogLevel Level, string Description, object? Value = null );
