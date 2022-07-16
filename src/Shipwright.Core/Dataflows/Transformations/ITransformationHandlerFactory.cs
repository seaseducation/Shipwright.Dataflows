// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Defines a factory for creating a transformation handler for an arbitrary transformation type.
/// </summary>
public interface ITransformationHandlerFactory
{
    /// <summary>
    /// Locates and invokes the factory for the given transformation.
    /// </summary>
    /// <param name="transformation">Transformation for which to create a handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The handler for the given transformation.</returns>
    public Task<ITransformationHandler> Create( Transformation transformation, CancellationToken cancellationToken );
}

/// <summary>
/// Defines a factory for creating handlers for a specific transformation type.
/// </summary>
/// <typeparam name="TTransformation">Type of transformation for which the handler can create factories.</typeparam>
public interface ITransformationHandlerFactory<TTransformation> where TTransformation : Transformation
{
    /// <summary>
    /// Builds a handler for the given transformation.
    /// </summary>
    /// <param name="transformation">Transformation for which to create a handler.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The handler for the given transformation.</returns>
    public Task<ITransformationHandler> Create( TTransformation transformation, CancellationToken cancellationToken );
}
