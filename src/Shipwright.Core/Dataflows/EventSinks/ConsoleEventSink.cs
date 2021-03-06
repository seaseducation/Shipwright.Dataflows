// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Shipwright.Dataflows.Sources;

namespace Shipwright.Dataflows.EventSinks;

/// <summary>
/// Event sink that logs information to the console.
/// </summary>
/// <param name="MinimumLevel">Minimum level of the events to log.</param>
public record ConsoleEventSink( LogLevel MinimumLevel = LogLevel.Warning ) : EventSink
{
    /// <summary>
    /// Validator for the <see cref="ConsoleEventSink"/>.
    /// </summary>
    public class Validator : AbstractValidator<ConsoleEventSink> {}

    /// <summary>
    /// Handler for the <see cref="ConsoleEventSink"/>.
    /// </summary>
    public class Handler : IEventSinkHandler
    {
        // ReSharper disable once InconsistentNaming
        internal readonly ILogger<Dataflow> _logger;
        // ReSharper disable once InconsistentNaming
        internal readonly Dataflow _dataflow;
        // ReSharper disable once InconsistentNaming
        internal readonly ConsoleEventSink _eventSink;


        public Handler( ILogger<Dataflow> logger, Dataflow dataflow, ConsoleEventSink eventSink )
        {
            _logger = logger ?? throw new ArgumentNullException( nameof(logger) );
            _dataflow = dataflow ?? throw new ArgumentNullException( nameof(dataflow) );
            _eventSink = eventSink ?? throw new ArgumentNullException( nameof(eventSink) );
        }

        public Task NotifyDataflowStarting( CancellationToken cancellationToken )
        {
            _logger.LogInformation( "Executing dataflow: {Dataflow}", _dataflow.Name );
            return Task.CompletedTask;
        }

        public Task NotifyDataflowCompleted( CancellationToken cancellationToken )
        {
            _logger.LogInformation( "Completed dataflow: {Dataflow}", _dataflow.Name );
            return Task.CompletedTask;
        }

        public Task NotifyRecordCompleted( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            foreach ( var e in record.Events )
            {
                if ( e.Level >= _eventSink.MinimumLevel )
                {
                    const string message = "Position: [{Position}] Source: [{Source}] Message: [{Message}] Values: [{Values}]";
                    _logger.Log( e.Level, message, record.Position, record.Source.Description, e.Description, JsonConvert.SerializeObject( e.Value ) );
                }
            }

            return Task.CompletedTask;
        }

        public Task NotifySourceStarting( Source source, CancellationToken cancellationToken )
        {
            if ( source == null ) throw new ArgumentNullException( nameof(source) );

            _logger.Log( LogLevel.Information, "Reading from data source [{Source}] beginning", source.Description );
            return Task.CompletedTask;
        }

        public Task NotifySourceCompleted( Source source, CancellationToken cancellationToken )
        {
            if ( source == null ) throw new ArgumentNullException( nameof(source) );

            foreach ( var e in source.Events )
            {
                if ( e.Level >= _eventSink.MinimumLevel )
                {
                    const string message = "Source: [{Source}] Message: [{Message}] StopProcessing: [{StopProcessing}] Values: [{Values}]";
                    _logger.Log( e.Level, message, source.Description, e.Description, e.StopProcessing, JsonConvert.SerializeObject( e.Value ) );
                }
            }

            _logger.Log( LogLevel.Information, "Reading from data source [{Source}] has ended", source.Description );
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Factory for the <see cref="ConsoleEventSink"/>.
    /// </summary>
    public class Factory : IEventSinkHandlerFactory<ConsoleEventSink>
    {
        readonly ILogger<Dataflow> _logger;

        public Factory( ILogger<Dataflow> logger )
        {
            _logger = logger ?? throw new ArgumentNullException( nameof(logger) );
        }

        public Task<IEventSinkHandler> Create( ConsoleEventSink eventSink, Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( eventSink == null ) throw new ArgumentNullException( nameof(eventSink) );
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

            IEventSinkHandler handler = new Handler( _logger, dataflow, eventSink );
            return Task.FromResult( handler );
        }
    }
}
