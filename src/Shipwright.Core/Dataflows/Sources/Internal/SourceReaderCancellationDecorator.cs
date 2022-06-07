// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using System.Runtime.CompilerServices;

namespace Shipwright.Dataflows.Sources.Internal;

/// <summary>
/// Decorates a data source reader to add cancellation support.
/// </summary>
public class SourceReaderCancellationDecorator : ISourceReader
{
    // ReSharper disable once InconsistentNaming
    internal readonly ISourceReader _inner;

    public SourceReaderCancellationDecorator( ISourceReader inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public async IAsyncEnumerable<Record> Read( [EnumeratorCancellation] CancellationToken cancellationToken )
    {
        await foreach ( var record in _inner.Read( cancellationToken ) )
        {
            if ( cancellationToken.IsCancellationRequested ) break;
            yield return record;
        }
    }
}
