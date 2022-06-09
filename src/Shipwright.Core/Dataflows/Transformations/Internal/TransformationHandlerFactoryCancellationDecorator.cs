// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.Internal;

/// <summary>
/// Decorates a transformation handler factory to add cancellation support.
/// </summary>
/// <typeparam name="TTransformation">Transformation whose handler factory to decorate.</typeparam>
public class TransformationHandlerFactoryCancellationDecorator<TTransformation> : ITransformationHandlerFactory<TTransformation> where TTransformation : Transformation
{
    readonly ITransformationHandlerFactory<TTransformation> _inner;

    public TransformationHandlerFactoryCancellationDecorator( ITransformationHandlerFactory<TTransformation> inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public async Task<ITransformationHandler> Create( TTransformation transformation, CancellationToken cancellationToken )
    {
        if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

        cancellationToken.ThrowIfCancellationRequested();
        return new TransformationHandlerCancellationDecorator( await _inner.Create( transformation, cancellationToken ) );
    }
}
