// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Requires that fields have a value.
/// </summary>
public record Required : Transformation
{
    /// <summary>
    /// Collection of fields that are required.
    /// </summary>
    public ICollection<string> Fields { get; init; } = new List<string>();

    /// <summary>
    /// Whether empty or whitespace text satisfies the required condition.
    /// Defaults to false.
    /// </summary>
    public bool AllowEmpty { get; init; } = false;

    /// <summary>
    /// Delegate for generating the log event to be applied to the record for missing values.
    /// </summary>
    public delegate LogEvent OnErrorDelegate( string field );

    /// <summary>
    /// Function for generating the log event to be applied to the record for missing values.
    /// </summary>
    public OnErrorDelegate OnError { get; init; } = field =>
        new( true, LogLevel.Error, $"[{field}] has no value; it is required." );

    /// <summary>
    /// Validator for the <see cref="Required"/> transformation.
    /// </summary>
    public class Validator : AbstractValidator<Required>
    {
        public Validator()
        {
            RuleFor( _ => _.Fields ).NotEmpty();
            RuleFor( _ => _.OnError ).NotNull();
        }
    }

    /// <summary>
    /// Handler for the <see cref="Required"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly Required _transformation;

        public Handler( Required transformation )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
        }

        public override Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            foreach ( var field in _transformation.Fields )
            {
                var missing = !record.TryGetValue( field, out var value );
                missing |= !_transformation.AllowEmpty && value is string text && string.IsNullOrWhiteSpace( text );

                if ( missing )
                {
                    record.Data.Remove( field );
                    record.Events.Add( _transformation.OnError( field ) );
                }
            }

            return Task.CompletedTask;
        }
    }

    public class Factory : ITransformationHandlerFactory<Required>
    {
        public Task<ITransformationHandler> Create( Required transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );
            return Task.FromResult<ITransformationHandler>( new Handler( transformation ) );
        }
    }
}
