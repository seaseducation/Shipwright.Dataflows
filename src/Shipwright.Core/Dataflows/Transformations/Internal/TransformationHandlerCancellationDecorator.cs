// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.Internal;

/// <summary>
/// Decorates a transformation handler to add cancellation support.
/// </summary>
public class TransformationHandlerCancellationDecorator : ITransformationHandler
{
    // ReSharper disable once InconsistentNaming
    internal readonly ITransformationHandler _inner;

    public TransformationHandlerCancellationDecorator( ITransformationHandler inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public ValueTask DisposeAsync() => _inner.DisposeAsync();

    public Task Transform( Record record, CancellationToken cancellationToken )
    {
        if ( record == null ) throw new ArgumentNullException( nameof(record) );

        cancellationToken.ThrowIfCancellationRequested();
        return _inner.Transform( record, cancellationToken );
    }
}
