// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using AutoFixture.Kernel;
using Microsoft.Extensions.Configuration;
using Shipwright.Databases;
using Shipwright.Dataflows.EventSinks;
using Shipwright.Dataflows.Sources;
using Shipwright.Dataflows.Transformations;

namespace Shipwright.Dataflows;

public static class AutoFixtureExtensions
{
    public static Fixture WithDataflowCustomization( this Fixture fixture )
    {
        fixture.Customizations.Add( new TypeRelay( typeof(Source), typeof(FakeSource) ) );
        fixture.Customizations.Add( new TypeRelay( typeof(Transformation), typeof(FakeTransformation) ) );
        fixture.Customizations.Add( new TypeRelay( typeof(EventSink), typeof(FakeEventSink) ) );
        fixture.Customizations.Add( new TypeRelay( typeof(DbConnectionInfo), typeof(FakeDbConnectionInfo) ) );
        fixture.Customize<Dataflow>( dataflow => dataflow.Without( _ => _.Configuration ) );

        return fixture;
    }
}
