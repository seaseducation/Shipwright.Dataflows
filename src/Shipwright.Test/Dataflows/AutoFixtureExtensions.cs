// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using AutoFixture.Kernel;
using Shipwright.Dataflows.Sources;

namespace Shipwright.Dataflows;

public static class AutoFixtureExtensions
{
    public static Fixture WithDataflowCustomization( this Fixture fixture )
    {
        fixture.Customizations.Add( new TypeRelay( typeof(Source), typeof(FakeSource) ) );
        return fixture;
    }
}
