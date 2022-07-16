// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.EventSinks.Internal;

/// <summary>
/// Decorates an event sink factory to add validation support.
/// </summary>
/// <typeparam name="TEventSink">Type of the event sink.</typeparam>
public class EventSinkHandlerFactoryValidationDecorator<TEventSink> : IEventSinkHandlerFactory<TEventSink> where TEventSink : EventSink
{
    readonly IEventSinkHandlerFactory<TEventSink> _inner;
    readonly IValidator<TEventSink> _validator;

    public EventSinkHandlerFactoryValidationDecorator( IEventSinkHandlerFactory<TEventSink> inner, IValidator<TEventSink> validator )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
        _validator = validator ?? throw new ArgumentNullException( nameof(validator) );
    }

    public async Task<IEventSinkHandler> Create( TEventSink eventSink, Dataflow dataflow, CancellationToken cancellationToken )
    {
        if ( eventSink == null ) throw new ArgumentNullException( nameof(eventSink) );
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

        var result = await _validator.ValidateAsync( eventSink, cancellationToken );

        if ( !result.IsValid )
            throw new ValidationException( $"Validation failed for event sink type {typeof(TEventSink)}", result.Errors );

        return await _inner.Create( eventSink, dataflow, cancellationToken );
    }
}
