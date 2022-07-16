// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.Internal;

/// <summary>
/// Decorates an <see cref="ITransformationHandlerFactory{TTransformation}"/> to add support for detecting events that
/// should stop record processing.
/// </summary>
/// <typeparam name="TTransformation">Type of transformation whose handler factory to decorate.</typeparam>
public class TransformationHandlerFactoryEventDecorator<TTransformation> : ITransformationHandlerFactory<TTransformation> where TTransformation: Transformation
{
    readonly ITransformationHandlerFactory<TTransformation> _inner;

    public TransformationHandlerFactoryEventDecorator( ITransformationHandlerFactory<TTransformation> inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public async Task<ITransformationHandler> Create( TTransformation transformation, CancellationToken cancellationToken )
    {
        if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

        return new TransformationHandlerEventDecorator( await _inner.Create( transformation, cancellationToken ) );
    }
}
