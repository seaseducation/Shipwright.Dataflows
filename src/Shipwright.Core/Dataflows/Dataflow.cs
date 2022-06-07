// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Identifiable;
using Shipwright.Commands;
using Shipwright.Dataflows.Sources;
using System.Threading.Tasks.Dataflow;

namespace Shipwright.Dataflows;

/// <summary>
/// Command that executes a dataflow.
/// </summary>
public record Dataflow : Command
{
    /// <summary>
    /// Unique name describing the dataflow.
    /// Logging for all dataflow instances will be grouped by this name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Maximum number of dataflow records to process at a time.
    /// Defaults to 1.
    /// Must be a positive value or -1 for unlimited.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 1;

    /// <summary>
    /// Collection of data sources from which to read records.
    /// </summary>
    public ICollection<Source> Sources { get; init; } = new List<Source>();

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
            RuleFor( _ => _.Sources ).NotEmpty();
        }
    }

    /// <summary>
    /// Helper class for handler execution.
    /// </summary>
    [UsedImplicitly]
    public class Helper
    {
        readonly ISourceReaderFactory _readerFactory;

        public Helper( ISourceReaderFactory readerFactory )
        {
            _readerFactory = readerFactory ?? throw new ArgumentNullException( nameof(readerFactory) );
        }

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

        public virtual Task<ISourceReader> GetSourceReader( IEnumerable<Source> sources, CancellationToken cancellationToken )
        {
            // todo: translate collection to an aggregate source
            throw new NotImplementedException();
        }
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

            var reader = await _helper.GetSourceReader( command.Sources, cts.Token );

            // send records to dataflow
            await foreach ( var record in reader.Read( cts.Token ) )
            {
                if ( cts.IsCancellationRequested ) break;
                await buffer.SendAsync( record, cts.Token );
            }

            buffer.Complete();
            await terminus.Completion;

            // todo: dataflow post-processing
        }
    }
}
