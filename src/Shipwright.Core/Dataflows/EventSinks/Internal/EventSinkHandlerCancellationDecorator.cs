// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Dataflows.Sources;

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

    public Task NotifyDataflowStarting( CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _inner.NotifyDataflowStarting( cancellationToken );
    }

    public Task NotifyDataflowCompleted( CancellationToken cancellationToken )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _inner.NotifyDataflowCompleted( cancellationToken );
    }

    public Task NotifyRecordCompleted( Record record, CancellationToken cancellationToken )
    {
        if ( record == null ) throw new ArgumentNullException( nameof(record) );

        cancellationToken.ThrowIfCancellationRequested();
        return _inner.NotifyRecordCompleted( record, cancellationToken );
    }

    public Task NotifySourceCompleted( Source source, CancellationToken cancellationToken )
    {
        if ( source == null ) throw new ArgumentNullException( nameof(source) );

        cancellationToken.ThrowIfCancellationRequested();
        return _inner.NotifySourceCompleted( source, cancellationToken );
    }
}
