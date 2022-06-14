// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Commands;

namespace Shipwright.Dataflows.Sources;

/// <summary>
/// Defines a dataflow record source.
/// </summary>
public abstract record Source : Command<ISourceReader>
{
    /// <summary>
    /// Optional description of the dataflow record source for logging.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Collection of log events that apply to the data source.
    /// </summary>
    public ICollection<LogEvent> Events { get; init; } = new List<LogEvent>();
}
