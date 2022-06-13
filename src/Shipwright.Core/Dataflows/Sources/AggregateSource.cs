// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentValidation;
using Shipwright.Dataflows.EventSinks;
using System.Runtime.CompilerServices;

namespace Shipwright.Dataflows.Sources;

/// <summary>
/// A data source that represents a collection of other data sources.
/// </summary>
public record AggregateSource : Source
{
    /// <summary>
    /// Collection of sources to aggregate.
    /// </summary>
    public ICollection<Source> Sources { get; init; } = new List<Source>();

    /// <summary>
    /// Validator for <see cref="AggregateSource"/>.
    /// </summary>
    public class Validator : AbstractValidator<AggregateSource>
    {
        public Validator()
        {
            RuleFor( _ => _.Sources ).NotEmpty();
        }
    }

    /// <summary>
    /// Reader for the <see cref="AggregateSource"/>.
    /// </summary>
    public class Reader : ISourceReader
    {
        // ReSharper disable once InconsistentNaming
        internal readonly IEnumerable<ISourceReader> _readers;

        public Reader( IEnumerable<ISourceReader> readers )
        {
            _readers = readers ?? throw new ArgumentNullException( nameof(readers) );
        }

        public async IAsyncEnumerable<Record> Read( IEventSinkHandler eventSinkHandler, [EnumeratorCancellation] CancellationToken cancellationToken )
        {
            if ( eventSinkHandler == null ) throw new ArgumentNullException( nameof(eventSinkHandler) );

            foreach ( var reader in _readers )
            {
                await foreach ( var record in reader.Read( eventSinkHandler, cancellationToken ) )
                {
                    yield return record;
                }
            }
        }
    }

    public class Factory : ISourceReaderFactory<AggregateSource>
    {
        readonly ISourceReaderFactory _factory;

        public Factory( ISourceReaderFactory factory )
        {
            _factory = factory ?? throw new ArgumentNullException( nameof(factory) );
        }

        public async Task<ISourceReader> Create( AggregateSource source, Dataflow dataflow, CancellationToken cancellationToken )
        {
            if ( source == null ) throw new ArgumentNullException( nameof(source) );
            if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

            var readers = new List<ISourceReader>();

            foreach ( var child in source.Sources )
            {
                readers.Add( await _factory.Create( child, dataflow, cancellationToken ) );
            }

            return new Reader( readers );
        }
    }
}
