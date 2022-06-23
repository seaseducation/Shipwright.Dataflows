// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Databases;

public record FakeDbConnectionInfo : DbConnectionInfo
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
