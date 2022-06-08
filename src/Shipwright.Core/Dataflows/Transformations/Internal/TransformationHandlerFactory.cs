// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Lamar;

namespace Shipwright.Dataflows.Transformations.Internal;

/// <summary>
/// Implementation of <see cref="ITransformationHandlerFactory"/> using Lamar.
/// </summary>
[UsedImplicitly]
public class TransformationHandlerFactory : ITransformationHandlerFactory
{
    readonly IServiceContext _container;

    public TransformationHandlerFactory( IServiceContext container )
    {
        _container = container ?? throw new ArgumentNullException( nameof(container) );
    }

    public async Task<ITransformationHandler> Create( Transformation transformation, Dataflow dataflow, CancellationToken cancellationToken )
    {
        if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

        var transformationType = transformation.GetType();
        var factoryType = typeof(ITransformationHandlerFactory<>).MakeGenericType( transformationType );
        dynamic factory = _container.GetInstance( factoryType );

        return await factory.Create( (dynamic)transformation, dataflow, cancellationToken );
    }
}
