// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Dataflows.Sources;

namespace Shipwright.Dataflows;

/// <summary>
/// Represents data being processed by a dataflow.
/// </summary>
public record Record
{
    /// <summary>
    /// Dictionary containing the current values of the record.
    /// </summary>
    public IDictionary<string,object?> Data { get; }

    /// <summary>
    /// Dictionary containing the original values of the record from the data source.
    /// </summary>
    public IReadOnlyDictionary<string,object?> Original { get; }

    /// <summary>
    /// Dataflow in which the record was generated.
    /// </summary>
    public Dataflow Dataflow { get; }

    /// <summary>
    /// Record source that produced the record.
    /// </summary>
    public Source Source { get; }

    /// <summary>
    /// Position of the record within the dataflow source.
    /// </summary>
    public long Position { get; }

    public Record( IDictionary<string,object?> data, Dataflow dataflow, Source source, long position )
    {
        if ( data == null ) throw new ArgumentNullException( nameof(data) );
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );
        if ( data == null ) throw new ArgumentNullException( nameof(data) );

        Data = new Dictionary<string, object?>( data, dataflow.FieldNameComparer );
        Original = new Dictionary<string, object?>( data, dataflow.FieldNameComparer );
        Dataflow = dataflow;
        Source = source ?? throw new ArgumentNullException( nameof(source) );
        Position = position;
    }

    /// <summary>
    /// Indexer for accessing data in the underlying data.
    /// </summary>
    /// <param name="key">Dictionary key.</param>
    public object? this[string key]
    {
        get => Data[key];
        set => Data[key] = value;
    }

    /// <summary>
    /// Collection of events that have been recorded against the record.
    /// </summary>
    public ICollection<LogEvent> Events { get; init; } = new List<LogEvent>();

    /// <summary>
    /// Returns whether the given field contains a non-null value.
    /// </summary>
    /// <param name="key">Key of the field whose value to check.</param>
    /// <param name="value">Value found, if applicable.</param>
    public bool TryGetValue( string key, out object value )
    {
        value = Data.TryGetValue( key, out var found )
            ? found!
            : null!;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        return value != null;
    }

    /// <summary>
    /// Returns the value if it is found in the record, otherwise returns the default value.
    /// </summary>
    /// <param name="key">Record field whose value to return.</param>
    /// <param name="default">Default value to return if none is found in the record. Defaults to null.</param>
    public object? GetValueOrDefault( string key, object? @default = null ) => TryGetValue( key, out var value )
        ? value
        : @default;
}
