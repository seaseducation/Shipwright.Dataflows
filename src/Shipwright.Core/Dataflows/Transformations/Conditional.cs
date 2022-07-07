// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Transformation that executes other transformations when record-specific conditions are met.
/// </summary>
public record Conditional : Transformation
{
    /// <summary>
    /// Defines a delegate for determining whether conditional transformations should execute.
    /// </summary>
    /// <param name="record">Dataflow record whose data to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public delegate Task<bool> ConditionalDelegate( Record record, CancellationToken cancellationToken );

    /// <summary>
    /// Delegate that determines whether the conditional transformations should execute.
    /// </summary>
    public ConditionalDelegate When { get; init; } = ( _, _ ) => Task.FromResult( false );

    /// <summary>
    /// Collection of transformations to execute when the condition is met.
    /// </summary>
    public ICollection<Transformation> Transformations { get; init; } = new List<Transformation>();

    /// <summary>
    /// Validator for the <see cref="Conditional"/> transformation.
    /// </summary>
    public class Validator : AbstractValidator<Conditional>
    {
        public Validator()
        {
            RuleFor( _ => _.When ).NotNull();
            RuleFor( _ => _.Transformations ).NotEmpty();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            When( _ => _.Transformations != null, () => RuleForEach( _ => _.Transformations ).NotNull() );
        }
    }

    /// <summary>
    /// Handler for the <see cref="Conditional"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly Conditional _transformation;
        internal readonly ITransformationHandler _inner;

        public Handler( Conditional transformation, ITransformationHandler inner )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
            _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            await base.DisposeAsyncCore();
            await _inner.DisposeAsync();
        }

        public override async Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            if ( await _transformation.When( record, cancellationToken ) )
                await _inner.Transform( record, cancellationToken );
        }
    }

    /// <summary>
    /// Factory for the <see cref="Conditional"/> transformation.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<Conditional>
    {
        readonly ITransformationHandlerFactory _transformationHandlerFactory;

        public Factory( ITransformationHandlerFactory transformationHandlerFactory )
        {
            _transformationHandlerFactory = transformationHandlerFactory ?? throw new ArgumentNullException( nameof(transformationHandlerFactory) );
        }

        public async Task<ITransformationHandler> Create( Conditional transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

            var aggregate = new AggregateTransformation { Transformations = transformation.Transformations };
            var inner = await _transformationHandlerFactory.Create( aggregate, cancellationToken );

            return new Handler( transformation, inner );
        }
    }
}
