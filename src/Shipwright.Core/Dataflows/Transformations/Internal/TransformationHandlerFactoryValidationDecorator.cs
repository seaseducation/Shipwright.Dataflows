// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.Transformations.Internal;

/// <summary>
/// Decorates a transformation handler factory to add validation support.
/// </summary>
/// <typeparam name="TTransformation">Type of transformation whose handler factory is decorated.</typeparam>
public class TransformationHandlerFactoryValidationDecorator<TTransformation> : ITransformationHandlerFactory<TTransformation> where TTransformation : Transformation
{
    readonly ITransformationHandlerFactory<TTransformation> _inner;
    readonly IValidator<TTransformation> _validator;

    public TransformationHandlerFactoryValidationDecorator( ITransformationHandlerFactory<TTransformation> inner, IValidator<TTransformation> validator )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
        _validator = validator ?? throw new ArgumentNullException( nameof(validator) );
    }

    public async Task<ITransformationHandler> Create( TTransformation transformation, CancellationToken cancellationToken )
    {
        if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

        var result = await _validator.ValidateAsync( transformation, cancellationToken );

        if ( !result.IsValid )
            throw new ValidationException( result.Errors );

        return await _inner.Create( transformation, cancellationToken );
    }
}
