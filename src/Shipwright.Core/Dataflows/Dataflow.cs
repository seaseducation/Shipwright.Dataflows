// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shipwright.Commands;
using Shipwright.Dataflows.EventSinks;
using Shipwright.Dataflows.Sources;
using Shipwright.Dataflows.Transformations;
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
    /// Comparer to use for field names.
    /// Defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>.
    /// </summary>
    public StringComparer FieldNameComparer { get; init; } = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Optional collection of fields that constitute key values in the dataflow.
    /// </summary>
    public ICollection<string> Keys { get; init; } = new List<string>();

    /// <summary>
    /// Collection of data sources from which to read records.
    /// </summary>
    public ICollection<Source> Sources { get; init; } = new List<Source>();

    /// <summary>
    /// Collection of transformations to execute against records in the dataflow.
    /// </summary>
    public ICollection<Transformation> Transformations { get; init; } = new List<Transformation>();

    /// <summary>
    /// Collection of event sinks to notify of dataflow events.
    /// </summary>
    public ICollection<EventSink> EventSinks { get; init; } = new List<EventSink>() { new ConsoleEventSink() };

    /// <summary>
    /// Configuration from which to obtain tenant-specific default values and replacements.
    /// </summary>
    public IConfiguration? Configuration { get; init; } = null;

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
            RuleFor( _ => _.Keys ).NotNull();
            RuleForEach( _ => _.Keys ).NotEmpty();
            RuleFor( _ => _.Sources ).NotEmpty();
            RuleFor( _ => _.Transformations ).NotEmpty();
            RuleFor( _ => _.EventSinks ).NotEmpty();
        }
    }

    /// <summary>
    /// Helper class for handler execution.
    /// </summary>
    [UsedImplicitly]
    public class Helper
    {
        readonly ISourceReaderFactory _readerFactory;
        readonly ITransformationHandlerFactory _transformationHandlerFactory;
        readonly IEventSinkHandlerFactory _eventSinkHandlerFactory;

        public Helper( ISourceReaderFactory readerFactory, ITransformationHandlerFactory transformationHandlerFactory, IEventSinkHandlerFactory eventSinkHandlerFactory )
        {
            _readerFactory = readerFactory ?? throw new ArgumentNullException( nameof(readerFactory) );
            _transformationHandlerFactory = transformationHandlerFactory ?? throw new ArgumentNullException( nameof(transformationHandlerFactory) );
            _eventSinkHandlerFactory = eventSinkHandlerFactory ?? throw new ArgumentNullException( nameof(eventSinkHandlerFactory) );
        }

        /// <summary>
        /// Creates a <see cref="CancellationTokenSource"/> linked to the given cancellation token.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for which to return a linked source.</param>
        public virtual CancellationTokenSource CreateLinkedTokenSource( CancellationToken cancellationToken ) =>
            CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );

        /// <summary>
        /// Builds the options to use for all dataflow blocks.
        /// </summary>
        /// <param name="dataflow">Dataflow command for which to build options.</param>
        /// <param name="cancellationToken">Cancellation token to include in options.</param>
        /// <returns>The completed dataflow options.</returns>
        public virtual ExecutionDataflowBlockOptions GetDataflowBlockOptions( Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );
            return new()
            {
                MaxDegreeOfParallelism = dataflow.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken,
                BoundedCapacity = dataflow.MaxDegreeOfParallelism,
            };
        }

        /// <summary>
        /// Builds the options to use for all dataflow links.
        /// </summary>
        public virtual DataflowLinkOptions GetDataflowLinkOptions() => new()
        {
            PropagateCompletion = true
        };

        /// <summary>
        /// Builds the data source reader for the given dataflow.
        /// </summary>
        /// <param name="dataflow">Dataflow whose data source reader to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The completed data source reader.</returns>
        public virtual Task<ISourceReader> GetSourceReader( Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );
            return _readerFactory.Create( new AggregateSource { Sources = dataflow.Sources }, dataflow, cancellationToken );
        }

        /// <summary>
        /// Returns <see cref="Required"/> transformations to ensure key fields are present.
        /// </summary>
        public virtual IEnumerable<Transformation> GetKeysRequired( Dataflow dataflow )
        {
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

            if ( dataflow.Keys.Any() )
            {
                yield return new Required
                {
                    AllowEmpty = false,
                    Fields = dataflow.Keys,
                    OnError = field => new( true, LogLevel.Error, $"Missing a required key field: {field}" )
                };
            }
        }

        /// <summary>
        /// Returns any <see cref="Replace"/> transformations defined in configuration.
        /// </summary>
        public virtual IEnumerable<Transformation> GetReplacements( Dataflow dataflow )
        {
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );
            if ( dataflow.Configuration == null ) yield break;

            foreach ( var replace in dataflow.Configuration.GetSection( "replace" ).GetChildren() )
            {
                var transformation = new Replace { Fields = { replace.Key } };
                var replacements = replace.Value != null ? dataflow.Configuration.GetSection( replace.Value ) : replace;

                foreach ( var replacement in replacements.GetChildren() )
                    transformation.Replacements.Add( new( replacement.Key, replacement.Value ) );

                if ( transformation.Replacements.Any() )
                    yield return transformation;
            }
        }

        /// <summary>
        /// Returns any <see cref="DefaultValue"/> transformations defined in configuration.
        /// </summary>
        public virtual IEnumerable<Transformation> GetDefaults( Dataflow dataflow )
        {
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );
            if ( dataflow.Configuration == null ) yield break;

            var transformation = new DefaultValue();

            foreach ( var @default in dataflow.Configuration.GetSection( "default" ).GetChildren() )
            {
                var field = @default.Key;
                var value = @default.Value;
                transformation.Defaults.Add( new( field, () => value ) );
            }

            if ( transformation.Defaults.Any() )
                yield return transformation;
        }

        /// <summary>
        /// Builds the dataflow transformation handler for the given dataflow.
        /// </summary>
        /// <param name="dataflow">Dataflow whose transformation handler to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The completed transformation handler.</returns>
        public virtual Task<ITransformationHandler> GetTransformationHandler( Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );
            var transformations = new List<Transformation>( dataflow.Transformations );

            transformations.InsertRange( 0, GetReplacements( dataflow ) );
            transformations.InsertRange( 0, GetDefaults( dataflow ) );
            transformations.InsertRange( 0, GetKeysRequired( dataflow ) );

            return _transformationHandlerFactory.Create( new AggregateTransformation { Transformations = transformations }, cancellationToken );
        }

        /// <summary>
        /// Builds the dataflow event sink handler for the given dataflow.
        /// </summary>
        /// <param name="dataflow">Dataflow whose event sink handler to create.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The completed event sink handler.</returns>
        public virtual Task<IEventSinkHandler> GetEventSinkHandler( Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );
            return _eventSinkHandlerFactory.Create( new AggregateEventSink { EventSinks = dataflow.EventSinks }, dataflow, cancellationToken );
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
            using var cts = _helper.CreateLinkedTokenSource( cancellationToken );

            var eventSinkHandler = await _helper.GetEventSinkHandler( command, cts.Token );
            var reader = await _helper.GetSourceReader( command, cts.Token );
            await using var transformationHandler = await _helper.GetTransformationHandler( command, cts.Token );

            var blockOptions = _helper.GetDataflowBlockOptions( command, cts.Token );
            var linkOptions = _helper.GetDataflowLinkOptions();

            async Task terminusAction( Record record )
            {
                try
                {
                    // execute per-record transformations
                    await transformationHandler.Transform( record, cts.Token );
                    await eventSinkHandler.NotifyRecordCompleted( record, cts.Token );
                }

                catch ( Exception e )
                {
                    // ReSharper disable once AccessToDisposedClosure
                    cts.Cancel();

                    // swallow cancellation exceptions - we're already cancelling
                    switch ( e )
                    {
                        case TaskCanceledException:
                        case OperationCanceledException: break;
                        default: throw;
                    }
                }
            }

            var buffer = new BufferBlock<Record>( blockOptions );
            var terminus = new ActionBlock<Record>( terminusAction, blockOptions );

            using var link = buffer.LinkTo( terminus, linkOptions );

            await eventSinkHandler.NotifyDataflowStarting( cts.Token );

            // send records to dataflow
            await foreach ( var record in reader.Read( eventSinkHandler, cts.Token ) )
            {
                if ( cts.IsCancellationRequested ) break;
                await buffer.SendAsync( record, cts.Token );
            }

            buffer.Complete();
            await terminus.Completion;

            await eventSinkHandler.NotifyDataflowCompleted( cts.Token );
        }
    }
}
