// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Dataflows.EventSinks;

namespace Shipwright.Dataflows.Sources;

/// <summary>
/// Defines a reader for getting dataflow records from a data source.
/// </summary>
public interface ISourceReader
{
    /// <summary>
    /// Reads records from the data source.
    /// </summary>
    /// <param name="eventSinkHandler">Event sink handler for notifications.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A stream of dataflow records from the source.</returns>
    public IAsyncEnumerable<Record> Read( IEventSinkHandler eventSinkHandler, CancellationToken cancellationToken );
}
