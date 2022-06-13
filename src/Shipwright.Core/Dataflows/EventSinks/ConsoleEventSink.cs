// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
        internal readonly LogLevel _minimumLevel;

        public Handler( ILogger<Dataflow> logger, LogLevel minimumLevel )
        {
            _logger = logger ?? throw new ArgumentNullException( nameof(logger) );
            _minimumLevel = minimumLevel;
        }

        public Task NotifyDataflowStarting( Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

            _logger.LogInformation( $"Executing dataflow: {dataflow.Name}" );
            return Task.CompletedTask;
        }

        public Task NotifyDataflowCompleted( Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

            _logger.LogInformation( $"Completed dataflow: {dataflow.Name}" );
            return Task.CompletedTask;
        }

        public Task NotifyRecordCompleted( Record record, CancellationToken cancellationToken )
        {
            if ( record == null ) throw new ArgumentNullException( nameof(record) );

            foreach ( var e in record.Events )
            {
                if ( e.Level >= _minimumLevel )
                {
                    var message = e.Value != null
                        ? $"{e.Description}: {JsonConvert.SerializeObject( e.Value )}"
                        : e.Description;

                    _logger.Log( e.Level, message );
                }
            }

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

        public Task<IEventSinkHandler> Create( ConsoleEventSink eventSink, CancellationToken cancellationToken )
        {
            if ( eventSink == null ) throw new ArgumentNullException( nameof(eventSink) );

            IEventSinkHandler handler = new Handler( _logger, eventSink.MinimumLevel );
            return Task.FromResult( handler );
        }
    }
}
