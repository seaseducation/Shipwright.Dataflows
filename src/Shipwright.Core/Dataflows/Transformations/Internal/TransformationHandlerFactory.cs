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

    public async Task<ITransformationHandler> Create( Transformation transformation, CancellationToken cancellationToken )
    {
        if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

        var transformationType = transformation.GetType();
        var factoryType = typeof(ITransformationHandlerFactory<>).MakeGenericType( transformationType );
        dynamic factory = _container.GetInstance( factoryType );

        return await factory.Create( (dynamic)transformation, cancellationToken );
    }
}
