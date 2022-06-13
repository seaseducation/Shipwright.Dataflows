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
}
