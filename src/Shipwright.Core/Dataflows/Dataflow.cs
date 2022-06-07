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
    /// Helper class for handler execution.
    /// </summary>
    [UsedImplicitly]
    public class Helper
    {
        /// <summary>
        /// Builds the options to use for all dataflow blocks.
        /// </summary>
        /// <param name="dataflow">Dataflow command for which to build options.</param>
        /// <param name="cancellationToken">Cancellation token to include in options.</param>
        /// <returns>The completed dataflow options.</returns>
        public virtual ExecutionDataflowBlockOptions GetDataflowBlockOptions( Dataflow dataflow, CancellationToken cancellationToken ) => new()
        {
            MaxDegreeOfParallelism = dataflow.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        /// <summary>
        /// Builds the options to use for all dataflow links.
        /// </summary>
        public virtual DataflowLinkOptions GetDataflowLinkOptions() => new()
        {
            PropagateCompletion = true
        };
    }

    /// <summary>
    /// Handler for the <see cref="Dataflow"/> command.
    /// </summary>
    [UsedImplicitly]
    public class Handler : CommandHandler<Dataflow>
    {
        readonly Helper _helper;

        public Handler( Helper helper )
        {
            _helper = helper ?? throw new ArgumentNullException( nameof(helper) );
        }

        protected override async Task ExecuteCommand( Dataflow command, CancellationToken cancellationToken )
        {
            if ( command == null ) throw new ArgumentNullException( nameof(command) );

            // get a source linked to the existing cancellation token
            // this will allow us to cancel the dataflow on unhandled exceptions
            using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );

            var blockOptions = _helper.GetDataflowBlockOptions( command, cts.Token );
            var linkOptions = _helper.GetDataflowLinkOptions();

            var buffer = new BufferBlock<Record>( blockOptions );
            var terminus = new ActionBlock<Record>( async record =>
            {
                try
                {
                    // todo: define per-record processing
                }

                catch ( Exception e )
                {
                    // ReSharper disable once AccessToDisposedClosure
                    cts.Cancel();
                    if ( e is not OperationCanceledException ) throw;
                }
            }, blockOptions );

            using var link = buffer.LinkTo( terminus, linkOptions );

            // todo: send records to dataflow

            buffer.Complete();
            await terminus.Completion;

            // todo: dataflow post-processing
        }
    }
}
