// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.EventSinks.Internal;

/// <summary>
/// Decorates a transformation handler factory to add cancellation support.
/// </summary>
/// <typeparam name="TEventSink">Event sink whose handler factory to decorate.</typeparam>
public class EventSinkHandlerFactoryCancellationDecorator<TEventSink> : IEventSinkHandlerFactory<TEventSink> where TEventSink : EventSink
{
    readonly IEventSinkHandlerFactory<TEventSink> _inner;

    public EventSinkHandlerFactoryCancellationDecorator( IEventSinkHandlerFactory<TEventSink> inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public async Task<IEventSinkHandler> Create( TEventSink eventSink, Dataflow dataflow, CancellationToken cancellationToken )
    {
        if ( eventSink == null ) throw new ArgumentNullException( nameof(eventSink) );
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

        cancellationToken.ThrowIfCancellationRequested();
        return new EventSinkHandlerCancellationDecorator( await _inner.Create( eventSink, dataflow, cancellationToken ) );
    }
}
