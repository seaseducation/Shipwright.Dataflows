// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows;

/// <summary>
/// Represents data being processed by a dataflow.
/// </summary>
/// <param name="Data">Dictionary that backs the record.</param>
public record Record( IDictionary<string,object?> Data )
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
