// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

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
    public EventDelegate OnFailed { get; init; } = field =>
        new( true, LogLevel.Error, $"Unable to convert the data in field [{field}]" );

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

    /// <summary>
    /// Delegate for performing an uppercase conversion using the invariant culture.
    /// </summary>
    public static ConverterDelegate ToUpperCase { get; } = ( object value, out object? converted ) =>
    {
        converted = value is string text
            ? text.ToUpperInvariant()
            : null;

        return converted != null;
    };

    /// <summary>
    /// Delegate for converting field values to <see cref="DateTime"/>.
    /// </summary>
    public static ConverterDelegate ToDateTime { get; } = ( object value, out object? converted ) =>
    {
        try
        {
            converted = value switch
            {
                DateTime dateTime => dateTime,
                string text when DateTime.TryParse( text, out var parsed ) => parsed,
                IConvertible convertible => Convert.ToDateTime( convertible ),
                _ => null
            };
        }
        catch
        {
            converted = null;
        }

        return converted != null;
    };

    /// <summary>
    /// Delegate for converting field values to <see cref="DateTime"/> with only a date component.
    /// </summary>
    public static ConverterDelegate ToDate { get; } = ( object value, out object? converted ) =>
    {
        converted = ToDateTime( value, out converted ) && converted is DateTime dateTime
            ? dateTime.Date
            : null;

        return converted != null;
    };

    /// <summary>
    /// Conventional values we want to add to
    /// </summary>
    static readonly IReadOnlyDictionary<string, bool> KnownBooleanStrings = new Dictionary<string, bool>( StringComparer.OrdinalIgnoreCase )
    {
        { "yes", true },
        { "no", false },
        { "y", true },
        { "n", false },
        { "0", false },
        { "1", true },
    };

    /// <summary>
    /// Delegate for converting field values to <see cref="Boolean"/>.
    /// </summary>
    public static ConverterDelegate ToBoolean { get; } = ( object value, out object? converted ) =>
    {
        try
        {
            converted = value switch
            {
                bool boolean => boolean,
                string text when bool.TryParse( text, out var parsed ) => parsed,
                string text when KnownBooleanStrings.TryGetValue( text, out var known ) => known,
                IConvertible convertible => Convert.ToBoolean( convertible ),
                _ => null
            };
        }
        catch
        {
            converted = null;
        }

        return converted != null;
    };

    /// <summary>
    /// Delegate for converting and formatting Social Security Numbers.
    /// </summary>
    public static ConverterDelegate ToSocialSecurityNumber { get; } = ( object value, out object? converted ) =>
    {
        converted = value switch
        {
            string candidate when ( candidate = Regex.Replace( candidate, "[^0-9]", string.Empty ) ).Length == 9  =>
                candidate.Insert( 3, "-" ).Insert( 6, "-" ),
            _ => null
        };

        return converted != null;
    };

    /// <summary>
    /// Delegate for converting a value to a decimal.
    /// </summary>
    public static ConverterDelegate ToDecimal { get; } = ( object value, out object? converted ) =>
    {
        try
        {
            converted = value switch
            {
                decimal number => number,
                string text when decimal.TryParse( text, out var parsed ) => parsed,
                IConvertible convertible => Convert.ToDecimal( convertible ),
                _ => null
            };
        }
        catch
        {
            converted = null;
        }

        return converted != null;
    };
}
