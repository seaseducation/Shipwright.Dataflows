// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Identifiable;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shipwright.Dataflows.Sources;
using System.Collections.Concurrent;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Transformation that enforces a uniqueness constraint on the dataflow.
/// </summary>
public record Unique : Transformation
{
    /// <summary>
    /// Collection of fields whose values must be unique within the dataflow.
    /// </summary>
    public ICollection<string> Fields { get; init; } = new List<string>();

    /// <summary>
    /// Whether comparison to other fields is case-sensitive.
    /// Defaults to false.
    /// </summary>
    public bool CaseSensitive { get; init; } = false;

    /// <summary>
    /// Defines a delegate for generating a log event for constraint failures.
    /// </summary>
    public delegate LogEvent FailureDelegate( Source source, long position );

    /// <summary>
    /// Delegate for creating record events when the constraint is violated.
    /// </summary>
    public FailureDelegate OnFailure { get; init; } = ( source, position ) => new(
        true,
        LogLevel.Error,
        $"Duplicate record detected",
        new { duplicateOf = new { source = source.ToString(), position } } );

    /// <summary>
    /// Validator for the <see cref="Unique"/> transformation.
    /// </summary>
    public class Validator : AbstractValidator<Unique>
    {
        public Validator()
        {
            RuleFor( _ => _.Fields ).NotEmpty();
            RuleForEach( _ => _.Fields ).NotEmpty();
            RuleFor( _ => _.OnFailure ).NotNull();
        }
    }

    /// <summary>
    /// Handler for the <see cref="Unique"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly Unique _transformation;
        readonly ConcurrentDictionary<Guid, (Source source, long position)> _keys = new();
        public Handler( Unique transformation )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
        }

        public override Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            var keyValues = new Dictionary<string, object?>( record.Dataflow.FieldNameComparer );

            foreach ( var field in _transformation.Fields )
            {
                if ( record.TryGetValue( field, out var value ) )
                    keyValues[field] = value;
            }

            var name = JsonConvert.SerializeObject( keyValues );

            // standardize when ignoring case
            if ( !_transformation.CaseSensitive )
                name = name.ToUpperInvariant();

            // build a record key from the data and get/add it to the key collection
            var key = NamedGuid.Compute( NamedGuidAlgorithm.SHA1, Guid.Empty, name );
            var (source, position) = _keys.GetOrAdd( key, (record.Source, record.Position) );

            // if this is not the first record with the key values, log the failure
            if ( ( source, position ) != ( record.Source, record.Position ) )
                record.Events.Add( _transformation.OnFailure( source, position ) );

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Factory for the <see cref="Unique"/> transformation.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<Unique>
    {
        public Task<ITransformationHandler> Create( Unique transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );
            return Task.FromResult<ITransformationHandler>( new Handler( transformation) );
        }
    }
}
