// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.Sources.Internal;

/// <summary>
/// Decorates a source reader factory to add validation support.
/// </summary>
/// <typeparam name="TSource">Type of data source whose reader factory is being decorated.</typeparam>
public class SourceReaderFactoryValidationDecorator<TSource> : ISourceReaderFactory<TSource> where TSource : Source
{
    readonly ISourceReaderFactory<TSource> _inner;
    readonly IValidator<TSource> _validator;

    public SourceReaderFactoryValidationDecorator( ISourceReaderFactory<TSource> inner, IValidator<TSource> validator )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
        _validator = validator ?? throw new ArgumentNullException( nameof(validator) );
    }

    public async Task<ISourceReader> Create( TSource source, Dataflow dataflow, CancellationToken cancellationToken )
    {
        if ( source == null ) throw new ArgumentNullException( nameof(source) );
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

        var result = await _validator.ValidateAsync( source, cancellationToken );

        if ( !result.IsValid )
            throw new ValidationException( $"Validation failed for source type {typeof(TSource)}", result.Errors );

        return await _inner.Create( source, dataflow, cancellationToken );
    }
}
