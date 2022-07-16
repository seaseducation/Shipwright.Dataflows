// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Dapper;
using FluentValidation;
using Shipwright.Databases;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Transformation that executes a database command.
/// </summary>
[UsedImplicitly]
public record DbCommand : Transformation
{
    /// <summary>
    /// <see cref="DbConnectionInfo"/> of the database where the command should be executed.
    /// </summary>
    public DbConnectionInfo ConnectionInfo { get; init; } = null!;

    /// <summary>
    /// Parameterized command to execute.
    /// Commands are expected to return no results.
    /// </summary>
    public string Sql { get; init; } = string.Empty;

    /// <summary>
    /// Type that maps a field to a database command parameter.
    /// </summary>
    /// <param name="Field">Dataflow field to map..</param>
    /// <param name="Parameter">Query parameter name. Defaults to the field name.</param>
    public record FieldMap( string Field, string? Parameter = null )
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        public class Validator : AbstractValidator<DbCommand.FieldMap>
        {
            public Validator()
            {
                RuleFor( _ => _.Field ).NotEmpty();
            }
        }
    }

    /// <summary>
    /// Collection of fields containing input values mapped to command input parameters.
    /// </summary>
    public ICollection<FieldMap> Input { get; init; } = new List<FieldMap>();

    /// <summary>
    /// Collection of additional parameters values, mapped by parameter name.
    /// </summary>
    public IDictionary<string, object?> Parameters { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Validator for the <see cref="DbCommand"/> transformation.
    /// </summary>
    public class Validator : AbstractValidator<DbCommand>
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

            RuleFor( _ => _.Parameters ).NotNull();
        }
    }

    /// <summary>
    /// Defines a helper for the <see cref="DbCommand"/> transformation.
    /// </summary>
    public interface IHelper
    {
        /// <summary>
        /// Executes the database command.
        /// </summary>
        /// <param name="parameters">Command parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task Execute( IDictionary<string, object?> parameters, CancellationToken cancellationToken );
    }

    /// <summary>
    /// Helper for the <see cref="DbCommand"/> transformation.
    /// </summary>
    public class Helper : IHelper
    {
        internal readonly DbCommand _transformation;
        internal readonly IDbConnectionFactory _connectionFactory;

        public Helper( DbCommand transformation, IDbConnectionFactory connectionFactory )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException( nameof(connectionFactory) );
        }

        public virtual async Task Execute( IDictionary<string,object?> parameters, CancellationToken cancellationToken )
        {
            var command = new CommandDefinition( _transformation.Sql, parameters: parameters, cancellationToken: cancellationToken );
            using var connection = _connectionFactory.Create( _transformation.ConnectionInfo );
            await connection.ExecuteAsync( command );
        }
    }

    /// <summary>
    /// Handler for the <see cref="DbCommand"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly DbCommand _transformation;
        internal readonly IHelper _helper;

        public Handler( DbCommand transformation, IHelper helper )
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

            await _helper.Execute( input, cancellationToken );
        }
    }

    /// <summary>
    /// Factory for the <see cref="DbCommand"/> transformation.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<DbCommand>
    {
        readonly IDbConnectionFactory _connectionFactory;

        public Factory( IDbConnectionFactory connectionFactory )
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException( nameof(connectionFactory) );
        }

        public Task<ITransformationHandler> Create( DbCommand transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

            var helper = new Helper( transformation, _connectionFactory );
            return Task.FromResult<ITransformationHandler>( new Handler( transformation, helper ) );
        }
    }
}
