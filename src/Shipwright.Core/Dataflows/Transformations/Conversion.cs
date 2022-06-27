// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Performs a data conversion on a record.
/// </summary>
public record Conversion : Transformation
{
    /// <summary>
    /// Collection of fields to convert.
    /// </summary>
    public ICollection<string> Fields { get; init; } = new List<string>();

    /// <summary>
    /// Delegate for attempting to convert a value.
    /// </summary>
    /// <param name="value">Value to convert.</param>
    /// <param name="result">Converted value when successful.</param>
    /// <returns>True when conversion is successful.</returns>
    public delegate bool ConverterDelegate( object value, out object? result );

    /// <summary>
    /// Delegate to use for converting values.
    /// </summary>
    public ConverterDelegate Converter { get; init; } = null!;

    /// <summary>
    /// Delegate for generating the log event for a field that failed to convert.
    /// </summary>
    public delegate LogEvent EventDelegate( string field );

    /// <summary>
    /// Failure event delegate to use on failure.
    /// </summary>
    public EventDelegate OnFailed { get; init; } = null!;

    /// <summary>
    /// Validator for the <see cref="Conversion"/> type.
    /// </summary>
    public class Validator : AbstractValidator<Conversion>
    {
        public Validator()
        {
            RuleFor( _ => _.Fields ).NotEmpty();
            RuleForEach( _ => _.Fields ).NotEmpty();
            RuleFor( _ => _.Converter ).NotNull();
            RuleFor( _ => _.OnFailed ).NotNull();
        }
    }

    /// <summary>
    /// Handler for the <see cref="Conversion"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly Conversion _transformation;

        public Handler( Conversion transformation )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
        }

        public override Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            foreach ( var field in _transformation.Fields )
            {
                if ( record.TryGetValue( field, out var value ) )
                {
                    if ( _transformation.Converter.Invoke( value, out var converted ) )
                    {
                        record[field] = converted;
                    }

                    else
                    {
                        record.Data.Remove( field );
                        record.Events.Add( _transformation.OnFailed( field ) with { Value = new { value } } );
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Factory for the <see cref="Conversion"/> transformation.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<Conversion>
    {
        public Task<ITransformationHandler> Create(  Conversion transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );
            return Task.FromResult<ITransformationHandler>( new Handler( transformation ) );
        }
    }
}
