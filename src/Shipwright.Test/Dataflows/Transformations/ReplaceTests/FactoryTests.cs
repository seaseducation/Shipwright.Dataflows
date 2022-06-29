// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.ReplaceTests;

public class FactoryTests
{
    ITransformationHandlerFactory<Replace> instance() => new Replace.Factory();

    public class Create : FactoryTests
    {
        Replace transformation = new Fixture().Create<Replace>();
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
            var handler = actual.Should().BeOfType<Replace.Handler>().Subject;
            handler._transformation.Should().BeSameAs( transformation );
        }
    }
}
