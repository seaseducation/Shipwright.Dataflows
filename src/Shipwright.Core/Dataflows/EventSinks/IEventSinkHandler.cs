// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Dataflows.Sources;

namespace Shipwright.Dataflows.EventSinks;

/// <summary>
/// Defines a handler for an event sink.
/// </summary>
public interface IEventSinkHandler
{
    /// <summary>
    /// Records that the associated dataflow is starting.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyDataflowStarting( CancellationToken cancellationToken );

    /// <summary>
    /// Records that the associated dataflow has completed execution.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyDataflowCompleted( CancellationToken cancellationToken );

    /// <summary>
    /// Records that the given record has completed execution within the associated dataflow.
    /// </summary>
    /// <param name="record">Record that has completed processing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyRecordCompleted( Record record, CancellationToken cancellationToken );

    /// <summary>
    /// Records that the given data source is starting to read within the associated dataflow.
    /// </summary>
    /// <param name="source">Dataflow record source that is starting to read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifySourceStarting( Source source, CancellationToken cancellationToken );

    /// <summary>
    /// Records that the given data source has completed reading within the associated dataflow.
    /// </summary>
    /// <param name="source">Dataflow record source that has completed reading.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifySourceCompleted( Source source, CancellationToken cancellationToken );
}
