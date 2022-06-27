// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.TruncateTests;

public class FactoryTests
{
    ITransformationHandlerFactory<Truncate> instance() => new Truncate.Factory();

    public class Create : FactoryTests
    {
        Truncate transformation = new Fixture().Create<Truncate>();
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
            var handler = actual.Should().BeOfType<Truncate.Handler>().Subject;
            handler._transformation.Should().BeSameAs( transformation );
        }
    }
}
