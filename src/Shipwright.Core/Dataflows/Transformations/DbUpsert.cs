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
    /// Validator for the <see cref="DbUpsert"/> transformation.
    /// </summary>
    public class Validator : AbstractValidator<DbUpsert>
    {
        public Validator()
        {
            RuleFor( _ => _.ConnectionInfo ).NotNull();
            RuleFor( _ => _.Table ).NotEmpty();
        }
    }
}
