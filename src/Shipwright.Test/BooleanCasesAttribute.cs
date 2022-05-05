// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using System.Reflection;
using Xunit.Sdk;

namespace Shipwright;

/// <summary>
/// Test cases of boolean values.
/// </summary>
public class BooleanCasesAttribute : DataAttribute
{
    public override IEnumerable<object[]> GetData( MethodInfo testMethod )
    {
        yield return new object[] { true };
        yield return new object[] { false };
    }
}
