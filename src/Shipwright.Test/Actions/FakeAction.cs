// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Commands;

namespace Shipwright.Actions;

public record FakeAction : Command
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
