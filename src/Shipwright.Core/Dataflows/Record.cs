// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Dataflows.Sources;

namespace Shipwright.Dataflows;

/// <summary>
/// Represents data being processed by a dataflow.
/// </summary>
/// <param name="Data">Dictionary that backs the record.</param>
/// <param name="Dataflow">Dataflow in which the record was generated.</param>
/// <param name="Source">Record source that produced the record.</param>
/// <param name="Position">Position of the record within the dataflow source.</param>
public record Record( IDictionary<string,object?> Data, Dataflow Dataflow, Source Source, long Position )
{
    /// <summary>
    /// Indexer for accessing data in the underlying data.
    /// </summary>
    /// <param name="key">Dictionary key.</param>
    public object? this[string key]
    {
        get => Data[key];
        set => Data[key] = value;
    }
}
