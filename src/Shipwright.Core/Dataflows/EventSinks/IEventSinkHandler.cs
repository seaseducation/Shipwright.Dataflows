// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.EventSinks;

/// <summary>
/// Defines a handler for an event sink.
/// </summary>
public interface IEventSinkHandler
{
    /// <summary>
    /// Records that the given dataflow is starting.
    /// </summary>
    /// <param name="dataflow">Dataflow whose execution is starting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyDataflowStarting( Dataflow dataflow, CancellationToken cancellationToken );

    /// <summary>
    /// Records that the given dataflow has completed execution.
    /// </summary>
    /// <param name="dataflow">Dataflow whose execution is complete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyDataflowCompleted( Dataflow dataflow, CancellationToken cancellationToken );

    /// <summary>
    /// Records that the given record has completed execution within the dataflow.
    /// </summary>
    /// <param name="record">Record that has completed processing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyRecordCompleted( Record record, CancellationToken cancellationToken );
}
