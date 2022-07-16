// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Commands;

namespace Shipwright.Dataflows.Sources;

public abstract record Source : Command<ISourceReader>
{
    /// <summary>
    /// Optional description of the dataflow record source for logging.
    /// </summary>
    public string? Description { get; init; }
}
