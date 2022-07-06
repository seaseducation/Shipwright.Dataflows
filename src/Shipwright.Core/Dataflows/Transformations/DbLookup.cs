// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Dapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Shipwright.Databases;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Transformation that performs a database lookup.
/// </summary>
public record DbLookup : Transformation
{
    /// <summary>
    /// <see cref="DbConnectionInfo"/> of the database to query.
    /// </summary>
    public DbConnectionInfo ConnectionInfo { get; init; } = null!;

    /// <summary>
    /// Parameterized query to execute.
    /// Lookup queries are expected to return exactly one row.
    /// </summary>
    public string Sql { get; init; } = string.Empty;

    /// <summary>
    /// Type that maps a field to a database query parameter.
    /// </summary>
    /// <param name="Field">Dataflow field to map..</param>
    /// <param name="Parameter">Query parameter name. Defaults to the field name.</param>
    public record FieldMap( string Field, string? Parameter = null )
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public class Validator : AbstractValidator<FieldMap>
        {
            public Validator()
            {
                RuleFor( _ => _.Field ).NotEmpty();
            }
        }
    }

    /// <summary>
    /// Collection of fields containing input values mapped to query input parameters.
    /// </summary>
    public ICollection<FieldMap> Input { get; init; } = new List<FieldMap>();

    /// <summary>
    /// Collection of fields that will receive output values mapped to query output parameters.
    /// </summary>
    public ICollection<FieldMap> Output { get; init; } = new List<FieldMap>();

    /// <summary>
    /// Collection of additional parameters values, mapped by parameter name.
    /// </summary>
    public IDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Defines a delegate for creating a <see cref="LogEvent"/> when a lookup fails.
    /// </summary>
    /// <param name="matches">Number of matching records found.</param>
    /// <param name="param">Object representing the parameter values used in the query.</param>
    public delegate LogEvent FailureDelegate( int matches, object param );

    /// <summary>
    /// Delegate that generates a <see cref="LogEvent"/> when the lookup fails for a record.
    /// </summary>
    public FailureDelegate OnFailure { get; init; } = ( count, param ) =>
        new( true, LogLevel.Error, $"Lookup failed: [{count}] records were returned", param );

    /// <summary>
    /// Validator for the <see cref="DbLookup"/> transformation.
    /// </summary>
    public class Validator : AbstractValidator<DbLookup>
    {
        public Validator()
        {
            var fieldMapValidator = new FieldMap.Validator();
            RuleFor( _ => _.ConnectionInfo ).NotNull();
            RuleFor( _ => _.Sql ).NotEmpty();
            RuleFor( _ => _.Input ).NotNull();

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            When( _ => _.Input != null, () =>
            {
                RuleFor( _ => _.Input )
                    .Must( maps => !maps.GroupBy( map => map?.Field ).Any( _ => _.Count() > 1 ) )
                    .WithMessage( $"{nameof(Input)} must contain unique field names." );
                RuleForEach( _ => _.Input ).NotNull().SetValidator( fieldMapValidator );
            });

            RuleFor( _ => _.Output ).NotNull();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            When( _ => _.Output != null, () =>
            {
                RuleFor( _ => _.Output )
                    .Must( maps => !maps.GroupBy( map => map?.Field ).Any( _ => _.Count() > 1 ) )
                    .WithMessage( $"{nameof(Output)} must contain unique field names." );
                RuleForEach( _ => _.Output ).NotNull().SetValidator( fieldMapValidator );
            });

            RuleFor( _ => _.Parameters ).NotNull();
            RuleFor( _ => _.OnFailure ).NotNull();
        }
    }

    /// <summary>
    /// Helper for the <see cref="DbLookup"/> transformation.
    /// </summary>
    public class Helper
    {
        internal readonly IDbConnectionFactory _connectionFactory;

        public Helper( IDbConnectionFactory connectionFactory )
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException( nameof(connectionFactory) );
        }

        public virtual async Task<IEnumerable<dynamic>> GetMatches( DbConnectionInfo connectionInfo, string sql, IDictionary<string,object?> parameters, CancellationToken cancellationToken )
        {
            var command = new CommandDefinition( sql, parameters: parameters, cancellationToken: cancellationToken );
            using var connection = _connectionFactory.Create( connectionInfo );
            return await connection.QueryAsync( command );
        }
    }

    /// <summary>
    /// Handler for the <see cref="DbLookup"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly DbLookup _transformation;
        internal readonly Helper _helper;

        public Handler( DbLookup transformation, Helper helper )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
            _helper = helper ?? throw new ArgumentNullException( nameof(helper) );
        }

        public override async Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            var input = new Dictionary<string, object?>( _transformation.Parameters );

            foreach ( var ( field, parameter ) in _transformation.Input )
            {
                input[parameter ?? field] = record.TryGetValue( field, out var value )
                    ? value
                    : null;
            }

            var matches = ( await _helper.GetMatches( _transformation.ConnectionInfo, _transformation.Sql, input, cancellationToken ) ).ToArray();

            if ( matches.Length == 1 )
            {
                IDictionary<string,object> result = matches.Single();

                foreach ( var ( field, parameter ) in _transformation.Output )
                {
                    if ( result.TryGetValue( parameter ?? field, out var value ) )
                        record[field] = value;
                }
            }

            else
            {
                record.Events.Add( _transformation.OnFailure( matches.Length, input ) );
            }
        }
    }

    /// <summary>
    /// Factory for the <see cref="DbLookup"/> transformation.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<DbLookup>
    {
        readonly IDbConnectionFactory _connectionFactory;

        public Factory( IDbConnectionFactory connectionFactory )
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException( nameof(connectionFactory) );
        }

        public Task<ITransformationHandler> Create( DbLookup transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

            var helper = new Helper( _connectionFactory );
            return Task.FromResult<ITransformationHandler>( new Handler( transformation, helper ) );
        }
    }
}
