// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations;

public record FakeTransformation : Transformation
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
