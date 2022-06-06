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
        protected override async Task ExecuteCommand( Dataflow command, CancellationToken cancellationToken )
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

            var buffer = new BufferBlock<Record>( executionOptions );
            var transform = new TransformBlock<Record, Record>( record =>
            {
                // todo: define record transformations
                return record;
            }, executionOptions );
            var terminus = new ActionBlock<Record>( record =>
            {
                // todo: define per-record post-processing
            }, executionOptions );

            using var transformLink = buffer.LinkTo( transform, linkOptions );
            using var terminusLink = transform.LinkTo( terminus, linkOptions );

            // todo: send records to dataflow

            buffer.Complete();
            await terminus.Completion;

            // todo: dataflow post-processing
        }
    }
}
