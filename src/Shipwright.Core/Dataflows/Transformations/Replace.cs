// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Transformation that performs value replacement.
/// </summary>
public record Replace : Transformation
{
    /// <summary>
    /// Collection of fields to which the mapping applies.
    /// </summary>
    public ICollection<string> Fields { get; init; } = new List<string>();

    /// <summary>
    /// Defines a value map.
    /// </summary>
    /// <param name="Incoming">Incoming value to replace (case sensitive).</param>
    /// <param name="Outgoing">Outgoing value to replace the incoming value with.</param>
    public record Replacement( string Incoming, object? Outgoing );

    /// <summary>
    /// Collection of keys (incoming values) to replace and the outgoing values to replace them with.
    /// Incoming values are case sensitive.
    /// </summary>
    public ICollection<Replacement> Replacements { get; init; } = new List<Replacement>();

    /// <summary>
    /// Validator for the <see cref="Replace"/> transformation.
    /// </summary>
    public class Validator : AbstractValidator<Replace>
    {
        class ValueMapValidator : AbstractValidator<Replacement>
        {
            public ValueMapValidator()
            {
                RuleFor( _ => _.Incoming ).NotEmpty();
            }
        }

        public Validator()
        {
            var valueMapValidator = new ValueMapValidator();
            RuleFor( _ => _.Fields ).NotEmpty();
            RuleForEach( _ => _.Fields ).NotEmpty();
            RuleFor( _ => _.Replacements ).NotEmpty();

            When( _ => _.Replacements?.Any() == true, () =>
            {
                RuleForEach( _ => _.Replacements ).SetValidator( valueMapValidator );
                RuleFor( _ => _.Replacements )
                    .Must( collection => collection.GroupBy( _ => _.Incoming ).Count() == collection.Count )
                    .WithMessage( ( tx, collection ) =>
                    {
                        var duplicates = collection.GroupBy( _ => _.Incoming ).Where( _ => _.Count() > 1 ).Select( _ => _.Key ).ToArray();
                        return $"{nameof(Replacements)} contains non-unique incoming values: {string.Join( ", ", duplicates )}";
                    } );
            } );
        }
    }

    /// <summary>
    /// Handler for the <see cref="Replace"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly Replace _transformation;
        readonly Dictionary<string, object?> _replacements;

        public Handler( Replace transformation )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
            _replacements = _transformation.Replacements.ToDictionary( _ => _.Incoming, _ => _.Outgoing );
        }

        public override Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            foreach ( var field in _transformation.Fields )
            {
                if ( record.TryGetValue( field, out var incoming ) )
                {
                    var incomingString = incoming as string ?? incoming.ToString();

                    if ( incomingString != null && _replacements.TryGetValue( incomingString, out var outgoing ) )
                    {
                        record[field] = outgoing;
                    }
                }
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Factory for the <see cref="Replace"/> transformation.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<Replace>
    {
        public Task<ITransformationHandler> Create( Replace transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );
            return Task.FromResult<ITransformationHandler>( new Handler( transformation ) );
        }
    }
}
