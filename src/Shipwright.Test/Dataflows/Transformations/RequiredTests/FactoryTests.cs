// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.RequiredTests;

public class FactoryTests
{
    ITransformationHandlerFactory<Required> instance() => new Required.Factory();

    public class Create : FactoryTests
    {
        Required transformation = new() { Fields = new Fixture().CreateMany<string>().ToList() };
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
            var handler = actual.Should().BeOfType<Required.Handler>().Subject;
            handler._transformation.Should().Be( transformation );
        }
    }
}
