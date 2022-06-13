// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Lamar;

namespace Shipwright.Dataflows.EventSinks.Internal;

/// <summary>
/// Implementation of <see cref="IEventSinkHandlerFactory"/> using Lamer.
/// </summary>
[UsedImplicitly]
public class EventSinkHandlerFactory : IEventSinkHandlerFactory
{
    readonly IServiceContext _container;

    public EventSinkHandlerFactory( IServiceContext container )
    {
        _container = container ?? throw new ArgumentNullException( nameof(container) );
    }

    public async Task<IEventSinkHandler> Create( EventSink eventSink, Dataflow dataflow, CancellationToken cancellationToken )
    {
        if ( eventSink == null ) throw new ArgumentNullException( nameof(eventSink) );
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

        var eventSinkType = eventSink.GetType();
        var factoryType = typeof(IEventSinkHandlerFactory<>).MakeGenericType( eventSinkType );
        dynamic factory = _container.GetInstance( factoryType );

        return await factory.Create( (dynamic)eventSink, dataflow, cancellationToken );
    }
}
