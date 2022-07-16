// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Commands;

public record FakeCommand : Command<Guid>
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
