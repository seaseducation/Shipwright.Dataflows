// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;
using Shipwright.Commands;

namespace Shipwright.Actions.Internal;

public abstract class CancellationActionDecoratorTests
{
    Mock<IActionFactory> inner = new( MockBehavior.Strict );
    IActionFactory instance() => new CancellationActionDecorator( inner?.Object! );

    public class Constructor : CancellationActionDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public abstract class Create : CancellationActionDecoratorTests
    {
        ActionContext context = new Fixture().Create<ActionContext>();
        CancellationToken cancellationToken;
        Task<Command> method() => instance().Create( context, cancellationToken );

        [Fact]
        public async Task requires_context()
        {
            context = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(context), method );
        }

        public class WhenCanceled : Create
        {
            [Fact]
            public async Task throws_operation_canceled()
            {
                cancellationToken = new( true );
                await Assert.ThrowsAsync<OperationCanceledException>( method );
            }
        }

        public class WhenNotCanceled : Create
        {
            [Fact]
            public async Task returns_created_action_from_inner_factory()
            {
                cancellationToken = new( false );

                var expected = new FakeAction();
                inner.Setup( _ => _.Create( context, cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                actual.Should().BeSameAs( expected );
            }
        }
    }
}
