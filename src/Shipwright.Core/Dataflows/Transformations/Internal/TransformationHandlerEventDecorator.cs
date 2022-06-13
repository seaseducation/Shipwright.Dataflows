// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.Internal;

/// <summary>
/// Decorates a transformation handler to inspect for any events that should stop processing.
/// </summary>
public class TransformationHandlerEventDecorator : ITransformationHandler
{
    // ReSharper disable once InconsistentNaming
    internal readonly ITransformationHandler _inner;

    public TransformationHandlerEventDecorator( ITransformationHandler inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public Task Transform( Record record, CancellationToken cancellationToken )
    {
        if ( record == null ) throw new ArgumentNullException( nameof(record) );

        return record.Events.Any( e => e.StopProcessing )
            ? Task.CompletedTask
            : _inner.Transform( record, cancellationToken );
    }

    public ValueTask DisposeAsync() => _inner.DisposeAsync();
}
