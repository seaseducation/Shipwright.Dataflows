// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.CodeTests;

public class FactoryTests
{
    ITransformationHandlerFactory<Code> instance() => new Code.Factory();

    public class Create : FactoryTests
    {
        Code transformation = new();
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
            var handler = actual.Should().BeOfType<Code.Handler>().Subject;
            handler._transformation.Should().BeSameAs( transformation );
        }
    }
}
