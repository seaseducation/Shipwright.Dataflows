// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using System.Reflection;
using Xunit.Sdk;

namespace Shipwright;

/// <summary>
/// Boolean test cases.
/// </summary>
public class BooleanCasesAttribute : DataAttribute
{
    public override IEnumerable<object[]> GetData( MethodInfo testMethod ) =>
        from value in new[] { true, false }
        select new object[] { value };
}
