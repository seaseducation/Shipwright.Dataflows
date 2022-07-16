// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations;

/// <summary>
/// Defines a handler for executing a transformation on a dataflow record.
/// Contains a default implementation of <see cref="IAsyncDisposable"/>.
/// </summary>
public abstract class TransformationHandler : ITransformationHandler
{
    /// <summary>
    /// Performs the asynchronous cleanup of managed resources or for cascading calls to DisposeAsync.
    /// Ensure that repeated calls to implementations of this method always succeed.
    /// See https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync#the-disposeasynccore-method
    /// </summary>
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    /// <summary>
    /// Performs asynchronous cleanup of managed resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize( this );
    }

    /// <summary>
    /// Executes the transformation on the given record.
    /// </summary>
    /// <param name="record">Record whose data to transform.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public abstract Task Transform( Record record, CancellationToken cancellationToken );
}
