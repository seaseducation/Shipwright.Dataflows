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
