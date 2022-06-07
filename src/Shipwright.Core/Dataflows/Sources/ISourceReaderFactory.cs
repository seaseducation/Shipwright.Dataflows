// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Sources;

/// <summary>
/// Defines a factory for creating a data source reader for a specific data source.
/// </summary>
/// <typeparam name="TSource">Type of data source for which the factory can create readers.</typeparam>
public interface ISourceReaderFactory<TSource>
{
    /// <summary>
    /// Creates a reader for the given data source.
    /// </summary>
    /// <param name="source">Source for which to create a reader.</param>
    /// <param name="dataflow">Dataflow for which the source is being read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created reader.</returns>
    public Task<ISourceReader> Create( TSource source, Dataflow dataflow, CancellationToken cancellationToken );
}

/// <summary>
/// Defines a factory for creating a data source reader for an arbitrary data source.
/// </summary>
public interface ISourceReaderFactory
{
    /// <summary>
    /// Locates a factory for the given data source and invokes it to create a reader.
    /// </summary>
    /// <param name="source">Source for which to create a reader.</param>
    /// <param name="dataflow">Dataflow for which the source is being reader.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created reader.</returns>
    public Task<ISourceReader> Create( Source source, Dataflow dataflow, CancellationToken cancellationToken );
}
