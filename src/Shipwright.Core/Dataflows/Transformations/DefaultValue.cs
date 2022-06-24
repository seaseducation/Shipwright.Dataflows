// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Transformation that applies default values to missing/empty fields.
/// </summary>
public record DefaultValue : Transformation
{
    /// <summary>
    /// Settings for each default.
    /// </summary>
    /// <param name="Field">Field for which a default value should be set.</param>
    /// <param name="Value">Value to use as a default.</param>
    public record Setting( string Field, Func<object?> Value )
    {
        /// <summary>
        /// Validator for <see cref="Setting"/>.
        /// </summary>
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public class Validator : AbstractValidator<Setting>
        {
            public Validator()
            {
                RuleFor( _ => _.Field ).NotEmpty();
                RuleFor( _ => _.Value ).NotNull();
            }
        }
    }

    /// <summary>
    /// Collection of defaults to set.
    /// </summary>
    public ICollection<Setting> Defaults { get; init; } = new List<Setting>();

    /// <summary>
    /// Whether to treat a whitespace value as empty.
    /// </summary>
    public bool DefaultOnBlank { get; init; } = true;

    /// <summary>
    /// Validator for <see cref="DefaultValue"/>.
    /// </summary>
    public class Validator : AbstractValidator<DefaultValue>
    {
        public Validator()
        {
            RuleFor( _ => _.Defaults ).NotEmpty();
            RuleForEach( _ => _.Defaults ).NotNull().SetValidator( new Setting.Validator() );
        }
    }

    /// <summary>
    /// Handler for <see cref="DefaultValue"/>.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly DefaultValue _transformation;

        public Handler( DefaultValue transformation )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
        }

        public override Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            foreach ( var ( field, value ) in _transformation.Defaults )
            {
                var empty = !record.TryGetValue( field, out var current );
                empty |= _transformation.DefaultOnBlank && current is string text && string.IsNullOrWhiteSpace( text );

                if ( empty )
                    record[field] = value();
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Handler factory for <see cref="DefaultValue"/>.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<DefaultValue>
    {
        public Task<ITransformationHandler> Create( DefaultValue transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

            return Task.FromResult<ITransformationHandler>( new Handler( transformation ) );
        }
    }
}
