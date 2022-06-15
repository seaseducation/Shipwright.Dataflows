// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using CsvHelper;
using CsvHelper.Configuration;
using FluentValidation;
using LamarCodeGeneration.Util;
using Microsoft.Extensions.Logging;
using Shipwright.Dataflows.EventSinks;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Shipwright.Dataflows.Sources;

/// <summary>
/// Data source that reads from character-separated-values (CSV) files.
/// </summary>
public record CsvSource : Source
{
    /// <summary>
    /// Path of the file to read.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Number of lines to skip before reading CSV data. This can be used to skip a header if it will not be used.
    /// Note: No CSV escaping is honored on the skipped lines.
    /// </summary>
    public int Skip { get; init; } = 0;

    public CsvConfiguration Configuration { get; init; } = new( CultureInfo.InvariantCulture );

    /// <summary>
    /// Validator for the <see cref="CsvSource"/>.
    /// </summary>
    public class Validator : AbstractValidator<CsvSource>
    {
        public Validator()
        {
            RuleFor( _ => _.Path ).NotEmpty();
            RuleFor( _ => _.Skip ).GreaterThanOrEqualTo( 0 );
            RuleFor( _ => _.Configuration ).NotNull();
        }
    }

    /// <summary>
    /// Reader for the <see cref="CsvSource"/>.
    /// </summary>
    public class Reader : ISourceReader
    {
        // ReSharper disable once InconsistentNaming
        internal readonly CsvSource _source;
        // ReSharper disable once InconsistentNaming
        internal readonly Dataflow _dataflow;
        // ReSharper disable once InconsistentNaming
        internal readonly ILogger<CsvSource> _logger;

        public Reader( CsvSource source, Dataflow dataflow, ILogger<CsvSource> logger )
        {
            _source = source ?? throw new ArgumentNullException( nameof(source) );
            _dataflow = dataflow ?? throw new ArgumentNullException( nameof(dataflow) );
            _logger = logger ?? throw new ArgumentNullException( nameof(logger) );
        }

        CsvConfiguration GetReadingConfiguration() => new( CultureInfo.InvariantCulture )
        {
            AllowComments = _source.Configuration.AllowComments,
            BadDataFound = args => throw new BadDataException( args.Context, $"Unescaped quote found on line {args.Context.Parser.RawRow}." ),
            Comment = _source.Configuration.Comment,
            Delimiter = _source.Configuration.Delimiter,
            DetectColumnCountChanges = true,
            DetectDelimiter = false,
            Encoding = Encoding.UTF8,
            Escape = _source.Configuration.Escape,
            HasHeaderRecord = _source.Configuration.HasHeaderRecord,
            IgnoreBlankLines = _source.Configuration.IgnoreBlankLines,
            Mode = CsvMode.RFC4180,
            NewLine = _source.Configuration.NewLine,
            Quote = _source.Configuration.Quote,
            TrimOptions = TrimOptions.Trim,
        };

        async IAsyncEnumerable<Record> GetRecordStream( bool preview, [EnumeratorCancellation] CancellationToken cancellationToken )
        {
            using var textReader = File.OpenText( _source.Path );
            bool canReadText() => !textReader.EndOfStream && !cancellationToken.IsCancellationRequested;

            // skip initial lines if needed
            // note: this does not honor any CSV settings, as CSV parsing is not yet active
            // it will skip exactly the number of CRLF, CR, or LF terminated lines
            for ( var skip = 0; skip < _source.Skip && canReadText(); skip++ )
                await textReader.ReadLineAsync();

            // terminate if the file is ended
            if ( textReader.EndOfStream ) yield break;

            // csv formatting will be honored from this point forward
            using var csvReader = new CsvReader( textReader, GetReadingConfiguration() );

            // read header record
            if ( csvReader.Configuration.HasHeaderRecord && !cancellationToken.IsCancellationRequested && await csvReader.ReadAsync() && csvReader.ReadHeader() )
            {
                for ( var i = 0; i < csvReader.HeaderRecord.Length; i++ )
                    csvReader.HeaderRecord[i] = string.IsNullOrWhiteSpace( csvReader.HeaderRecord[i] )
                        ? $"Field_{i}"
                        : csvReader.HeaderRecord[i].Trim();

                var duplicates = csvReader.HeaderRecord
                    .GroupBy( header => header )
                    .Where( _ => _.Count() > 1 )
                    .Select( _ => _.Key )
                    .ToList();

                if ( duplicates.Any() )
                    throw new BadDataException( csvReader.Context, $"Duplicate header name: {duplicates.First()}." );
            }

            // read all records
            while ( !cancellationToken.IsCancellationRequested && await csvReader.ReadAsync() )
            {
                var data = new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase );

                for ( var i = 0; i < csvReader.ColumnCount; i++ )
                {
                    var field = csvReader.HeaderRecord?[i] ?? $"Field_{i}";
                    var value = csvReader[i];
                    data[field] = !string.IsNullOrWhiteSpace( value ) ? value : null;
                }

                if ( !preview )
                    yield return new( data, _dataflow, _source, csvReader.Context.Parser.RawRow );
            }
        }

        async IAsyncEnumerable<Record> IterateRecordStream( bool preview, [EnumeratorCancellation] CancellationToken cancellationToken )
        {
            await using var enumerator = GetRecordStream( preview, cancellationToken ).GetAsyncEnumerator( cancellationToken );

            for ( var more = true; more; )
            {
                try
                {
                    more = await enumerator.MoveNextAsync();
                }
                catch ( Exception ex )
                {
                    _source.Events.Add( new( true, LogLevel.Critical, ex.Message )
                    {
                        Value = ex switch
                        {
                            FileNotFoundException fnf => new { _source.Path },
                            BadDataException bad => new { _source.Path, bad.Context.Parser.RawRow },
                            _ => new { ex.StackTrace }
                        }
                    } );

                    _logger.LogCritical( ex, "Error reading CSV file {Path}", _source.Path );
                    break;
                }

                if ( more ) yield return enumerator.Current;
            }
        }

        public async IAsyncEnumerable<Record> Read( IEventSinkHandler eventSinkHandler, [EnumeratorCancellation] CancellationToken cancellationToken )
        {
            if ( eventSinkHandler == null ) throw new ArgumentNullException( nameof(eventSinkHandler) );

            await eventSinkHandler.NotifySourceStarting( _source, cancellationToken );

            // run a pass in preview to capture any file errors
            await foreach ( var _ in IterateRecordStream( true, cancellationToken ) ) {}

            // if no stop errors were found in the file, read the record stream
            if ( !_source.Events.Any( _ => _.StopProcessing ) )
            {
                await foreach ( var record in IterateRecordStream( false, cancellationToken ) )
                    yield return record;
            }

            await eventSinkHandler.NotifySourceCompleted( _source, cancellationToken );
        }
    }

    /// <summary>
    /// Factory to create a reader for the <see cref="CsvSource"/>.
    /// </summary>
    public class Factory : ISourceReaderFactory<CsvSource>
    {
        readonly ILogger<CsvSource> _logger;

        public Factory( ILogger<CsvSource> logger )
        {
            _logger = logger ?? throw new ArgumentNullException( nameof(logger) );
        }

        public Task<ISourceReader> Create( CsvSource source, Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( source == null ) throw new ArgumentNullException( nameof(source) );
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

            return Task.FromResult<ISourceReader>( new Reader( source, dataflow, _logger ) );
        }
    }
}
