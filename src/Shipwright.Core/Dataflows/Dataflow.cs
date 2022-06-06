// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Shipwright.Commands;

namespace Shipwright.Dataflows;

/// <summary>
/// Command that executes a dataflow.
/// </summary>
public record Dataflow : Command
{
    /// <summary>
    /// Unique name describing the dataflow.
    /// Logging for all records within a dataflow will be grouped by this name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Validator for the <see cref="Dataflow"/> command.
    /// </summary>
    [UsedImplicitly]
    public class Validator : AbstractValidator<Dataflow>
    {
        public Validator()
        {
            RuleFor( _ => _.Name ).NotEmpty();
        }
    }

    /// <summary>
    /// Handler for the <see cref="Dataflow"/> command.
    /// </summary>
    [UsedImplicitly]
    public class Handler : CommandHandler<Dataflow>
    {
        protected override Task ExecuteCommand( Dataflow command, CancellationToken cancellationToken )
        {
            if ( command == null ) throw new ArgumentNullException( nameof(command) );
            throw new NotImplementedException();
        }
    }
}
