// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Transformation that represents a collection of other transformations.
/// </summary>
public record AggregateTransformation : Transformation
{
    /// <summary>
    /// Collection of transformations to execute.
    /// </summary>
    public ICollection<Transformation> Transformations { get; init; } = new List<Transformation>();

    /// <summary>
    /// Validator for the <see cref="AggregateTransformation"/>.
    /// </summary>
    [UsedImplicitly]
    public class Validator : AbstractValidator<AggregateTransformation>
    {
        public Validator()
        {
            RuleFor( _ => _.Transformations ).NotEmpty();
        }
    }

    /// <summary>
    /// Handler for the <see cref="AggregateTransformation"/>.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly IEnumerable<ITransformationHandler> _handlers;

        public Handler( IEnumerable<ITransformationHandler> handlers )
        {
            _handlers = handlers ?? throw new ArgumentNullException( nameof(handlers) );
        }

        protected override async ValueTask DisposeAsyncCore()
        {
            await base.DisposeAsyncCore();

            foreach ( var handler in _handlers )
            {
                await handler.DisposeAsync();
            }
        }

        public override async Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            foreach ( var handler in _handlers )
            {
                await handler.Transform( record, cancellationToken );
            }
        }
    }

    /// <summary>
    /// Factory for the <see cref="AggregateTransformation"/>.
    /// </summary>
    [UsedImplicitly]
    public class Factory : ITransformationHandlerFactory<AggregateTransformation>
    {
        readonly ITransformationHandlerFactory _factory;

        public Factory( ITransformationHandlerFactory factory )
        {
            _factory = factory ?? throw new ArgumentNullException( nameof(factory) );
        }

        public async Task<ITransformationHandler> Create( AggregateTransformation transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

            var handlers = new List<ITransformationHandler>();
            var success = false;

            try
            {
                foreach ( var child in transformation.Transformations )
                {
                    handlers.Add( await _factory.Create( child, cancellationToken ) );
                }

                success = true;
            }

            // ensure any created handlers are disposed if we run into an error
            finally
            {
                if ( !success )
                {
                    foreach ( var handler in handlers )
                    {
                        await handler.DisposeAsync();
                    }
                }
            }

            return new Handler( handlers );
        }
    }
}
