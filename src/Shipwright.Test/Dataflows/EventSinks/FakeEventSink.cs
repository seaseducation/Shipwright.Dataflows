// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.EventSinks;

public record FakeEventSink : EventSink
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
