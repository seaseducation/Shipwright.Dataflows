// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Core.Commands.Internal;

public class CommandDispatcherTests
{
    Mock<IServiceProvider>? serviceProvider = new( MockBehavior.Strict );
    ICommandDispatcher instance() => new CommandDispatcher( serviceProvider?.Object! );

    public class Constructor : CommandDispatcherTests
    {
        [Fact]
        public void requires_serviceProvider()
        {
            serviceProvider = null!;
            Assert.Throws<ArgumentNullException>( nameof(serviceProvider), instance );
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

        public class WhenServiceLocated : Execute
        {
            [Theory]
            [BooleanCases]
            public async Task returns_result_from_executing_located_handler( bool canceled )
            {
                cancellationToken = new( canceled );

                var expected = Guid.NewGuid();
                var handler = new Mock<ICommandHandler<FakeCommand, Guid>>( MockBehavior.Strict );
                handler.Setup( _ => _.Execute( command, cancellationToken ) ).ReturnsAsync( expected );
                serviceProvider?.Setup( _ => _.GetService( typeof(ICommandHandler<FakeCommand, Guid>) ) ).Returns( handler.Object );

                var actual = await method();
                actual.Should().Be( expected );
            }
        }

        public class WhenServiceNotLocated : Execute
        {
            [Theory]
            [BooleanCases]
            public async Task throws_invalid_operation( bool canceled )
            {
                cancellationToken = new( canceled );
                serviceProvider?.Setup( _ => _.GetService( typeof(ICommandHandler<FakeCommand, Guid>) ) ).Returns( null );

                await Assert.ThrowsAsync<InvalidOperationException>( method );
            }
        }
    }
}
