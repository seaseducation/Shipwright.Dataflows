// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.EventSinks.Internal;

/// <summary>
/// Decorates an event sink handler to add cancellation support.
/// </summary>
public class EventSinkHandlerCancellationDecorator : IEventSinkHandler
{
    // ReSharper disable once InconsistentNaming
    internal readonly IEventSinkHandler _inner;

    public EventSinkHandlerCancellationDecorator( IEventSinkHandler inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public Task NotifyDataflowStarting( Dataflow dataflow, CancellationToken cancellationToken )
    {
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

        cancellationToken.ThrowIfCancellationRequested();
        return _inner.NotifyDataflowStarting( dataflow, cancellationToken );
    }

    public Task NotifyDataflowCompleted( Dataflow dataflow, CancellationToken cancellationToken )
    {
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

        cancellationToken.ThrowIfCancellationRequested();
        return _inner.NotifyDataflowCompleted( dataflow, cancellationToken );
    }

    public Task NotifyRecordCompleted( Record record, CancellationToken cancellationToken )
    {
        if ( record == null ) throw new ArgumentNullException( nameof(record) );

        cancellationToken.ThrowIfCancellationRequested();
        return _inner.NotifyRecordCompleted( record, cancellationToken );
    }
}
