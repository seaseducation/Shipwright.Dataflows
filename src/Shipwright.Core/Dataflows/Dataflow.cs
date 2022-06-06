// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Shipwright.Commands;
using System.Threading.Tasks.Dataflow;

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
    /// Maximum number of dataflow records to process at a time.
    /// Defaults to 1.
    /// Must be a positive value or -1 for unlimited.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 1;

    /// <summary>
    /// Validator for the <see cref="Dataflow"/> command.
    /// </summary>
    [UsedImplicitly]
    public class Validator : AbstractValidator<Dataflow>
    {
        public Validator()
        {
            RuleFor( _ => _.Name ).NotEmpty();
            RuleFor( _ => _.MaxDegreeOfParallelism ).GreaterThan( 0 ).When( _ => _.MaxDegreeOfParallelism != -1 );
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

            var executionOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = command.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken,
            };

            var linkOptions = new DataflowLinkOptions
            {
                PropagateCompletion = true,
            };

            throw new NotImplementedException();
        }
    }
}
