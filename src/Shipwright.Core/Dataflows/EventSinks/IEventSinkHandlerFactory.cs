// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.EventSinks;

/// <summary>
/// Defines a factory for creating a handlers for a specific event sink.
/// </summary>
/// <typeparam name="TEventSink">Type of the event sink for which the factory can create a handler.</typeparam>
public interface IEventSinkHandlerFactory<TEventSink> where TEventSink : EventSink
{
    /// <summary>
    /// Creates a handler for the given event sink.
    /// </summary>
    /// <param name="eventSink">Event sink for which to create a handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<IEventSinkHandler> Create( TEventSink eventSink, CancellationToken cancellationToken );
}

/// <summary>
/// Defines a factory for creating handlers for arbitrary event sinks.
/// </summary>
public interface IEventSinkHandlerFactory
{
    /// <summary>
    /// Creates a handler for the given event sink.
    /// </summary>
    /// <param name="eventSink">Event sink for which to create a handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<IEventSinkHandler> Create( EventSink eventSink, CancellationToken cancellationToken );
}
