// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.DefaultValueTests;

public class FactoryTests
{
    ITransformationHandlerFactory<DefaultValue> instance() => new DefaultValue.Factory();

    public class Create : FactoryTests
    {
        DefaultValue transformation = new Fixture().Create<DefaultValue>();
        Task<ITransformationHandler> method() => instance().Create( transformation, default );

        [Fact]
        public async Task requires_transformation()
        {
            transformation = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(transformation), method );
        }

        [Fact]
        public async Task returns_handler()
        {
            var actual = await method();
            var handler = actual.Should().BeOfType<DefaultValue.Handler>().Subject;
            handler._transformation.Should().BeSameAs( transformation );
        }
    }
}
