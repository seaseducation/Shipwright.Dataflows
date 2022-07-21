// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Shipwright.Databases;

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
    public record FieldMap( ColumnType Type, string Field, string Column );

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

            RuleFor( _ => _.Fields )
                .Must( fields => fields?.GroupBy( _ => _?.Column ?? string.Empty )?.All( _ => _.Count() == 1 ) == true )
                .WithMessage( "Duplicate column name found in field mappings" );

            RuleFor( _ => _.Fields )
                .Must( fields => fields?.Any( _ => _.Type == ColumnType.Key ) == true )
                .WithMessage( "No key column found in field mappings" );
        }
    }
}
