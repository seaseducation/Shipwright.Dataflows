// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Shipwright.Dataflows.Sources;

namespace Shipwright.Dataflows;

public record FakeSource : Source
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
