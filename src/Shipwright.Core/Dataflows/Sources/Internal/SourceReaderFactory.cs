// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Lamar;

namespace Shipwright.Dataflows.Sources.Internal;

/// <summary>
/// Implementation of <see cref="ISourceReaderFactory" /> using Lamar.
/// </summary>
[UsedImplicitly]
public class SourceReaderFactory : ISourceReaderFactory
{
    readonly IServiceContext container;

    public SourceReaderFactory( IServiceContext container )
    {
        this.container = container ?? throw new ArgumentNullException( nameof(container) );
    }

    public async Task<ISourceReader> Create( Source source, Dataflow dataflow, CancellationToken cancellationToken )
    {
        if ( source == null ) throw new ArgumentNullException( nameof(source) );
        if ( dataflow == null ) throw new ArgumentNullException( nameof(dataflow) );

        var sourceType = source.GetType();
        var factoryType = typeof(ISourceReaderFactory<>).MakeGenericType( sourceType );
        dynamic factory = container.GetInstance( factoryType );

        return await factory.Create( (dynamic)source, dataflow, cancellationToken );
    }
}
