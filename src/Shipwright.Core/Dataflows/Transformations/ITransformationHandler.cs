// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Defines a handler for executing a transformation on a dataflow record.
/// </summary>
public interface ITransformationHandler : IAsyncDisposable
{
    /// <summary>
    /// Executes the transformation on the given record.
    /// </summary>
    /// <param name="record">Record whose data to transform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task Transform( Record record, CancellationToken cancellationToken );
}
