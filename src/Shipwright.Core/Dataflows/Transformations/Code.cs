// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Transformation that executes an arbitrary code delegate.
/// </summary>
public record Code : Transformation
{
    /// <summary>
    /// Defines a delegate for for a transformation.
    /// </summary>
    public delegate Task CodeDelegate( Record record, CancellationToken cancellationToken );

    /// <summary>
    /// Delegate to execute against dataflow records.
    /// </summary>
    public CodeDelegate Delegate { get; init; } = null!;

    /// <summary>
    /// Validator for the <see cref="Code"/> transformation.
    /// </summary>
    public class Validator : AbstractValidator<Code>
    {
        public Validator() => RuleFor( _ => _.Delegate ).NotNull();
    }

    /// <summary>
    /// Handler for the <see cref="Code"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly Code _transformation;

        public Handler( Code transformation )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
        }

        public override async Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );
            await _transformation.Delegate.Invoke( record, cancellationToken );
        }
    }

    /// <summary>
    /// Factory for the <see cref="Code"/> transformation.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<Code>
    {
        public Task<ITransformationHandler> Create( Code transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );
            return Task.FromResult<ITransformationHandler>( new Handler( transformation ) );
        }
    }
}
