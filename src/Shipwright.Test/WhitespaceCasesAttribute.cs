// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using System.Reflection;
using Xunit.Sdk;

namespace Shipwright;

/// <summary>
/// Test cases for empty and whitespace values.
/// </summary>
public class WhitespaceCasesAttribute : DataAttribute
{
    /// <summary>
    /// String value composed of all common whitespace characters.
    /// </summary>

    static readonly string WhiteSpace = new string(
        new char[]
        {
            '\x0009', // horizontal tab
            '\x000a', // line feed
            '\x000b', // vertical tab
            '\x000c', // form feed
            '\x000d', // carriage return
            '\x0085', // next line
            '\x00a0', // non-breaking space
        } );

    public override IEnumerable<object[]> GetData( MethodInfo testMethod )
    {
        var values = new [] { string.Empty, WhiteSpace };

        foreach ( var value in values )
            yield return new object[] { value };
    }
}
