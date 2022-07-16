// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Sources.Internal;

/// <summary>
/// Decorates a data source reader factory to add cancellation support.
/// </summary>
/// <typeparam name="TSource">Source whose factory is decorated.</typeparam>
public class SourceReaderFactoryCancellationDecorator<TSource> : ISourceReaderFactory<TSource> where TSource : Source
{
    readonly ISourceReaderFactory<TSource> _inner;

    public SourceReaderFactoryCancellationDecorator( ISourceReaderFactory<TSource> inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public async Task<ISourceReader> Create( TSource source, Dataflow dataflow, CancellationToken cancellationToken )
    {
        if ( source == null ) throw new ArgumentNullException( nameof(source) );
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

        cancellationToken.ThrowIfCancellationRequested();

        var reader = await _inner.Create( source, dataflow, cancellationToken );
        return new SourceReaderCancellationDecorator( reader );
    }
}
