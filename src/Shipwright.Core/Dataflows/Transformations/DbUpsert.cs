// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Identifiable;
using Newtonsoft.Json;
using Shipwright.Databases;
using SqlKata.Compilers;
using SqlKata.Execution;
using System.Collections;

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Transformation that updates database records, inserting new ones if they do not exist.
/// </summary>
[UsedImplicitly]
public record DbUpsert : Transformation
{
    /// <summary>
    /// Connection information for the target database.
    /// </summary>

    public DbConnectionInfo ConnectionInfo { get; init; } = null!;

    /// <summary>
    /// Database table whose records to update.
    /// This can be a quoted name.
    /// </summary>
    public string Table { get; init; } = null!;

    /// <summary>
    /// Collection of fields mapped to columns that participate in inserts/updates.
    /// At least one key field is required.
    /// Columns may be mapped only once.
    /// </summary>
    public ICollection<FieldMap> Fields { get; init; } = new List<FieldMap>();

    /// <summary>
    /// Column participation type.
    /// </summary>
    public enum ColumnType
    {
        /// <summary>
        /// Key columns are used to locate the existing record.
        /// Their values are inserted for new records, but never updated.
        /// </summary>
        Key,

        /// <summary>
        /// Insert columns are inserted for new records, but never updated.
        /// Useful for record insert metadata.
        /// </summary>
        Insert,

        /// <summary>
        /// Update columns are inserted for new records, and updated when their values have changed.
        /// </summary>
        Update,

        /// <summary>
        /// Trigger columns are inserted for new records, and only updated when an Update column value has changed.
        /// Useful for record update metadata.
        /// </summary>
        Trigger,
    }

    /// <summary>
    /// Maps a dataflow record field to a database column.
    /// </summary>
    /// <param name="Type">Type of the column mapping.</param>
    /// <param name="Field">Dataflow record field name.</param>
    /// <param name="Column">Database column name.</param>
    public record FieldMap( ColumnType Type, string Field, string Column )
    {
        /// <summary>
        /// Defines a delegate for determining if a change detected in a column value should result in updating that
        /// column.
        /// </summary>
        public delegate bool ShouldReplaceDelegate( object? incoming, object? existing );

        /// <summary>
        /// Delegate for determining if a change detected ina column value should result in updating that column.
        /// </summary>
        public ShouldReplaceDelegate Replace { get; init; } = ( _, _ ) => true;

        /// <summary>
        /// Deconstruct overload.
        /// </summary>
        public void Deconstruct( out ColumnType type, out string field, out string column, out ShouldReplaceDelegate replace ) =>
            ( type, field, column, replace ) = (Type, Field, Column, Replace);
    }

    /// <summary>
    /// Validator for the <see cref="DbUpsert"/> transformation.
    /// </summary>
    public class Validator : AbstractValidator<DbUpsert>
    {
        public Validator()
        {
            RuleFor( _ => _.ConnectionInfo ).NotNull();
            RuleFor( _ => _.Table ).NotEmpty();
            RuleFor( _ => _.Fields ).NotEmpty();
            RuleForEach( _ => _.Fields ).NotNull();

            RuleForEach( _ => _.Fields ).Where( field => field != null )
                .Must( _ => !string.IsNullOrWhiteSpace( _.Field ) )
                .WithMessage( "Field mapping {CollectionIndex} requires a field name" );

            RuleForEach( _ => _.Fields ).Where( field => field != null )
                .Must( _ => !string.IsNullOrWhiteSpace( _.Column ) )
                .WithMessage( "Field mapping {CollectionIndex} requires a column name" );

            RuleForEach( _ => _.Fields ).Where( field => field != null )
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                .Must( _ => _.Replace != null )
                .WithMessage( "Field mapping {CollectionIndex} requires a replacement evaluator" );

            RuleFor( _ => _.Fields )
                .Must( fields => fields?.GroupBy( _ => _?.Column ?? string.Empty )?.All( _ => _.Count() == 1 ) == true )
                .WithMessage( "Duplicate column name found in field mappings" );

            RuleFor( _ => _.Fields )
                .Must( fields => fields?.Any( _ => _.Type == ColumnType.Key ) == true )
                .WithMessage( "No key column found in field mappings" );
        }
    }

    /// <summary>
    /// Handler for the <see cref="DbUpsert"/> transformation.
    /// </summary>
    public class Handler : TransformationHandler
    {
        internal readonly DbUpsert _transformation;
        internal readonly IDbConnectionFactory _connectionFactory;
        internal readonly Compiler _compiler;

        public Handler( DbUpsert transformation, IDbConnectionFactory connectionFactory, Compiler compiler )
        {
            _transformation = transformation ?? throw new ArgumentNullException( nameof(transformation) );
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException( nameof(connectionFactory) );
            _compiler = compiler ?? throw new ArgumentNullException( nameof(compiler) );
        }

        /// <summary>
        /// Wrapper for a semaphore that maintains a count of the number of records referencing it.
        /// </summary>
        internal class SemaphoreCounter
        {
            public int Count { get; set; } = 1;
            public SemaphoreSlim Semaphore { get; } = new( 1, 1 );
        }

        /// <summary>
        /// Collection of semaphores indexed by record key identifier.
        /// All reads and writes should be locked.
        /// </summary>
        internal readonly Dictionary<Guid, SemaphoreCounter> _semaphores = new();

        /// <summary>
        /// Safely returns the semaphore for the given record identifier.
        /// </summary>
        /// <param name="id">Record key identifier whose semaphore to return.</param>
        public virtual SemaphoreSlim GetSemaphore( Guid id )
        {
            lock ( _semaphores )
            {
                if ( _semaphores.TryGetValue( id, out var counter ) )
                {
                    ++counter.Count;
                    return counter.Semaphore;
                }

                return (_semaphores[id] = new()).Semaphore;
            }
        }

        /// <summary>
        /// Releases the specified semaphore and decrements its counter.
        /// </summary>
        /// <param name="id">Record identifier of the semaphore to release.</param>
        public virtual void ReleaseSemaphore( Guid id )
        {
            lock ( _semaphores )
            {
                var counter = _semaphores[id];
                counter.Semaphore.Release();

                // remove from the collection if there are no more references
                if ( --counter.Count == 0 )
                {
                    _semaphores.Remove( id );
                    counter.Semaphore.Dispose();
                }
            }
        }

        /// <summary>
        /// Wrapper that releases the semaphore for the specified record when disposed.
        /// </summary>
        /// <param name="Id">Record whose semaphore to release.</param>
        /// <param name="Callback">Callback for releasing the semaphore.</param>
        internal record Releaser( Guid Id, Action<Guid> Callback ) : IDisposable
        {
            public void Dispose() => Callback( Id );
        }

        /// <summary>
        /// Computes a deterministic identifier based on record key values.
        /// </summary>
        /// <param name="record">Record whose identifier to compute.</param>
        public virtual Guid GetRecordIdentifier( Record record )
        {
            var values = new Dictionary<string, object?>();

            // populate all field values
            foreach ( var ( type, field, column ) in _transformation.Fields )
                if ( type == ColumnType.Key )
                    values[column] = record.GetValueOrDefault( field );

            // compute the record key
            var name = JsonConvert.SerializeObject( values ).ToUpperInvariant();
            return NamedGuid.Compute( NamedGuidAlgorithm.SHA1, Guid.Empty, name );
        }

        /// <summary>
        /// Obtains an exclusive lock on the record key to ensure that only one record with a given key can be
        /// processing through the handler at a time. This helps avoid deadlocks.
        /// </summary>
        /// <param name="record">Record containing the key values.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A reference that releases the lock when disposed.</returns>
        public virtual async Task<IDisposable> Lock( Record record, CancellationToken cancellationToken )
        {
            var id = GetRecordIdentifier( record );
            var semaphore = GetSemaphore( id );
            await semaphore.WaitAsync( cancellationToken );
            return new Releaser( id, ReleaseSemaphore );
        }

        /// <summary>
        /// Queries the database for an existing record.
        /// </summary>
        /// <param name="selectable">Columns to select in the query.</param>
        /// <param name="parameters">Query parameters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The collection of database records matching the key values.</returns>
        public virtual async Task<IEnumerable<dynamic>> Select( IEnumerable<string> selectable, IDictionary<string,object?> parameters, CancellationToken cancellationToken )
        {
            using var connection = _connectionFactory.Create( _transformation.ConnectionInfo );
            var db = new QueryFactory( connection, _compiler );
            var query = db.Query( _transformation.Table );
            return await query.Where( parameters ).Select( selectable.ToArray() ).GetAsync( cancellationToken: cancellationToken );
        }

        /// <summary>
        /// Queries the database for an existing record with the key values.
        /// </summary>
        /// <param name="record">Record whose existing database record to query.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The collection of database records matching the key values.</returns>
        public virtual async Task<IEnumerable<dynamic>> Select( Record record, CancellationToken cancellationToken )
        {
            var selectable = new List<string>();
            var parameters = new Dictionary<string, object?>();

            foreach ( var ( type, field, column ) in _transformation.Fields )
            {
                selectable.Add( column );

                if ( type == ColumnType.Key )
                    parameters[column] = record.GetValueOrDefault( field );
            }

            return await Select( selectable, parameters, cancellationToken );
        }

        /// <summary>
        /// Returns whether two values are equivalent.
        /// </summary>
        /// <param name="incoming">First value to compare.</param>
        /// <param name="existing">Second value to compare.</param>
        /// <returns>True if the values are equivalent; otherwise false.</returns>
        public virtual bool AreEqual( object? incoming, object? existing ) => incoming switch
        {
            // null comparisons
            null => existing == null,

            // array comparisons (e.g., MSSQL varbinary)
            IStructuralEquatable structural when structural.GetType().IsArray =>
                structural.Equals( existing, StructuralComparisons.StructuralEqualityComparer ),

            // boolean/integer conversions
            // handles edge cases where boolean values are stored as integers
            bool boolSource when existing is int intExisting =>
                ( boolSource && intExisting == 1 ) || ( !boolSource && intExisting == 0 ),

            // decimal/numeric conversions
            // handles edge cases in oracle where dapper reads a numeric value into an unexpected data type
            decimal decimalSource when existing is IConvertible convertible =>
                decimalSource.Equals( Convert.ToDecimal( convertible ) ),

            // standard equality
            _ => Equals( incoming, existing )
        };

        /// <summary>
        /// Determines whether the existing database record should be updated with changes from the dataflow record.
        /// </summary>
        /// <param name="record">Record containing values to compare against the existing database record.</param>
        /// <param name="existing">Existing database record.</param>
        /// <param name="changes">The columns with detected changes and their values.</param>
        /// <returns>True if the record should be updated with changes; false if the values are unchanged.</returns>
        public virtual bool TryGetChanges( Record record, IDictionary<string,object?> existing, out Dictionary<string,object?> changes )
        {
            changes = new();

            foreach ( var ( type, field, column, replace ) in _transformation.Fields )
            {
                if ( type == ColumnType.Update )
                {
                    var incoming = record.GetValueOrDefault( field );
                    var current = existing.TryGetValue( column, out var value ) ? value : null;

                    if ( !AreEqual( incoming, current ) && replace( incoming, current ) )
                        changes[column] = incoming;
                }
            }

            return changes.Any();
        }

        /// <summary>
        /// Inserts a new record into the database.
        /// </summary>
        /// <param name="values">Record values indexed by column name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public virtual async Task Insert( Dictionary<string,object?> values, CancellationToken cancellationToken )
        {
            using var connection = _connectionFactory.Create( _transformation.ConnectionInfo );
            var db = new QueryFactory( connection, _compiler );
            var query = db.Query( _transformation.Table );
            await query.InsertAsync( values, cancellationToken: cancellationToken );
        }

        /// <summary>
        /// Inserts a new record into the database.
        /// </summary>
        /// <param name="record">Record containing the values to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public virtual async Task Insert( Record record, CancellationToken cancellationToken )
        {
            var values = new Dictionary<string, object?>();

            foreach ( var (_, field, column) in _transformation.Fields )
                values[column] = record.GetValueOrDefault( field );

            await Insert( values, cancellationToken );
        }

        /// <summary>
        /// Updates an existing record in the database.
        /// </summary>
        /// <param name="record">Record containing incoming values.</param>
        /// <param name="changes">Collection of changed columns and their values.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public virtual Task Update( Record record, IDictionary<string,object?> changes, CancellationToken cancellationToken )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Implementation of <see cref="ITransformationHandler"/>.
        /// </summary>
        public override async Task Transform( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            using var @lock = await Lock( record, cancellationToken );
            var matches = ( await Select( record, cancellationToken ) ).ToArray();
            if ( matches.Length > 1 ) throw new InvalidOperationException( "Multiple matches found for the DbUpsert key value" );

            IDictionary<string, object?>? existing = matches.SingleOrDefault();

            if ( existing == null )
                await Insert( record, cancellationToken );
            else if ( TryGetChanges( record, existing, out var changes ) )
                await Update( record, changes, cancellationToken );
        }
    }

    /// <summary>
    /// Factory for the <see cref="DbUpsert"/> transformation handler.
    /// </summary>
    public class Factory : ITransformationHandlerFactory<DbUpsert>
    {
        readonly IDbConnectionFactory _connectionFactory;

        public Factory( IDbConnectionFactory connectionFactory )
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException( nameof(connectionFactory) );
        }

        public Task<ITransformationHandler> Create( DbUpsert transformation, CancellationToken cancellationToken )
        {
            if ( transformation == null ) throw new ArgumentNullException( nameof(transformation) );

            var compiler = transformation.ConnectionInfo switch
            {
                OracleConnectionInfo => new OracleCompiler(),
                _ => throw new NotImplementedException( $"No compiler configured for the connection type: {transformation.ConnectionInfo.GetType()}" )
            };

            return Task.FromResult<ITransformationHandler>( new Handler( transformation, _connectionFactory, compiler ) );
        }
    }
}
