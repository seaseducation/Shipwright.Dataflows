// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.Transformations;

public record Truncate : Transformation
{
    /// <summary>
    /// Field settings for the <see cref="Truncate"/>.
    /// </summary>
    /// <param name="Field">Name of the field to truncate.</param>
    /// <param name="Length">Maximum allowed length of the field.</param>
    public record Setting( string Field, int Length )
    {
        /// <summary>
        /// Validator for the <see cref="Setting"/> type.
        /// </summary>
        public class Validator : AbstractValidator<Setting>
        {
            public Validator()
            {
                RuleFor( _ => _.Field ).NotEmpty();
                RuleFor( _ => _.Length ).GreaterThan( 0 );
            }
        }
    }

    /// <summary>
    /// Collection of field settings for the transformation.
    /// </summary>
    public ICollection<Setting> Fields { get; init; } = new List<Setting>();

    /// <summary>
    /// Validator for the <see cref="Truncate"/> type.
    /// </summary>
    public class Validator : AbstractValidator<Truncate>
    {
        public Validator()
        {
            var settingsValidator = new Setting.Validator();
            RuleFor( _ => _.Fields ).NotEmpty();
            RuleForEach( _ => _.Fields ).SetValidator( settingsValidator );
        }
    }

    /// <summary>
    /// Handler for the <see cref="Truncate"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly Truncate _transformation;

        public Handler( Truncate transformation )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
        }

        public override Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            foreach ( var ( field, length ) in _transformation.Fields )
            {
                if ( record.TryGetValue( field, out var value ) && value is string text && text.Length > length )
                    record[field] = text[..length];
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Factory for the <see cref="Truncate"/> transformation.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<Truncate>
    {
        public Task<ITransformationHandler> Create( Truncate transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );
            return Task.FromResult<ITransformationHandler>( new Handler( transformation ) );
        }
    }
}
