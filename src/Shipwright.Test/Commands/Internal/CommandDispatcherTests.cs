// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Lamar;

namespace Shipwright.Commands.Internal;

public abstract class CommandDispatcherTests
{
    Mock<IServiceContext> container = new( MockBehavior.Strict );
    ICommandDispatcher instance() => new CommandDispatcher( container?.Object! );

    public class Constructor : CommandDispatcherTests
    {
        [Fact]
        public void requires_container()
        {
            container = null!;
            Assert.Throws<ArgumentNullException>( nameof(container), instance );
        }
    }

    public abstract class Execute : CommandDispatcherTests
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
            public async Task returns_result_of_executing_resolved_handler()
            {
                var expected = Guid.NewGuid();
                var handler = new Mock<ICommandHandler<FakeCommand, Guid>>( MockBehavior.Strict );
                handler.Setup( _ => _.Execute( command, cancellationToken ) ).ReturnsAsync( expected );

                container.Setup( _ => _.GetInstance( typeof(ICommandHandler<FakeCommand, Guid>) ) )
                    .Returns( handler.Object );

                var actual = await method();
                actual.Should().Be( expected );
            }
        }
    }
}
