// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Core.Commands.Decorators;

public abstract class CancellationCommandDecoratorTests
{
    Mock<ICommandHandler<FakeCommand, Guid>> inner = new( MockBehavior.Strict );
    ICommandHandler<FakeCommand, Guid> instance() => new CancellationCommandDecorator<FakeCommand, Guid>( inner?.Object! );

    public class Constructor : CancellationCommandDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public abstract class Execute : CancellationCommandDecoratorTests
    {
        FakeCommand command = new();
        CancellationToken cancellationToken;
        Task<Guid> method() => instance().Execute( command, cancellationToken );

        [Fact]
        public async Task requires_command()
        {
            command = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(command), method );
        }

        public class WhenCanceled : Execute
        {
            [Fact]
            public async Task throws_operation_canceled()
            {
                cancellationToken = new( true );
                await Assert.ThrowsAsync<OperationCanceledException>( method );
            }
        }

        public class WhenNotCanceled : Execute
        {
            [Fact]
            public async Task returns_result_from_executed_inner_handler()
            {
                cancellationToken = new( false );

                var expected = Guid.NewGuid();
                inner.Setup( _ => _.Execute( command, cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                actual.Should().Be( expected );
            }
        }
    }
}
