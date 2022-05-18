// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Commands;

namespace Shipwright.Actions.Internal;

/// <summary>
/// Decorates an <see cref="IActionFactory"/> to add pre-create cancellation support.
/// </summary>
public class CancellationActionDecorator : IActionFactory
{
    readonly IActionFactory _inner;

    public CancellationActionDecorator( IActionFactory inner )
    {
        _inner = inner ?? throw new ArgumentNullException( nameof(inner) );
    }

    public Task<Command> Create( ActionContext context, CancellationToken cancellationToken )
    {
        if ( context == null ) throw new ArgumentNullException( nameof(context) );

        cancellationToken.ThrowIfCancellationRequested();
        return _inner.Create( context, cancellationToken );
    }
}
