// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Shipwright.Dataflows.Sources;

namespace Shipwright.Dataflows.EventSinks;

/// <summary>
/// An event sink that aggregates the functionality of multiple event sinks.
/// </summary>
public record AggregateEventSink : EventSink
{
    /// <summary>
    /// Collection of event sinks to aggregate.
    /// </summary>
    public ICollection<EventSink> EventSinks { get; init; } = new List<EventSink>();

    /// <summary>
    /// Validator for the <see cref="AggregateEventSink"/>.
    /// </summary>
    public class Validator : AbstractValidator<AggregateEventSink>
    {
        public Validator()
        {
            RuleFor( _ => _.EventSinks ).NotEmpty();
        }
    }

    /// <summary>
    /// Handler for the <see cref="AggregateEventSink"/>.
    /// </summary>
    public class Handler : IEventSinkHandler
    {
        // ReSharper disable once InconsistentNaming
        internal readonly IEnumerable<IEventSinkHandler> _handlers;

        public Handler( IEnumerable<IEventSinkHandler> handlers )
        {
            _handlers = handlers ?? throw new ArgumentNullException( nameof(handlers) );
        }

        public async Task NotifyDataflowStarting( CancellationToken cancellationToken )
        {
            foreach ( var handler in _handlers )
                await handler.NotifyDataflowStarting( cancellationToken );
        }

        public async Task NotifyDataflowCompleted( CancellationToken cancellationToken )
        {
            foreach ( var handler in _handlers )
                await handler.NotifyDataflowCompleted( cancellationToken );
        }

        public async Task NotifyRecordCompleted( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            foreach ( var handler in _handlers )
                await handler.NotifyRecordCompleted( record, cancellationToken );
        }

        public async Task NotifySourceCompleted( Source source, CancellationToken cancellationToken )
        {
            if ( source == null ) throw new ArgumentNullException( nameof(source) );

            foreach ( var handler in _handlers )
                await handler.NotifySourceCompleted( source, cancellationToken );
        }
    }

    /// <summary>
    /// Factory for the <see cref="AggregateEventSink"/>.
    /// </summary>
    public class Factory : IEventSinkHandlerFactory<AggregateEventSink>
    {
        readonly IEventSinkHandlerFactory _factory;

        public Factory( IEventSinkHandlerFactory factory )
        {
            _factory = factory ?? throw new ArgumentNullException( nameof(factory) );
        }

        public async Task<IEventSinkHandler> Create( AggregateEventSink eventSink, Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( eventSink == null ) throw new ArgumentNullException( nameof(eventSink) );
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

            var handlers = new List<IEventSinkHandler>();

            foreach ( var child in eventSink.EventSinks )
            {
                handlers.Add( await _factory.Create( child, dataflow, cancellationToken ) );
            }

            return new Handler( handlers );
        }
    }
}
