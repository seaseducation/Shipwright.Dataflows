// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;
using Lamar;
using Microsoft.Extensions.Configuration;
using Shipwright.Commands;

namespace Shipwright.Actions.Internal.InvokeActionTests;

public abstract class HandlerTests
{
    Mock<IServiceContext> container = new( MockBehavior.Strict );
    Mock<IActionSettings> settings = new( MockBehavior.Strict );
    ICommandHandler<InvokeAction> instance() => new InvokeAction.Handler( container?.Object!, settings?.Object! );

    public class Constructor : HandlerTests
    {
        [Fact]
        public void requires_container()
        {
            container = null!;
            Assert.Throws<ArgumentNullException>( nameof(container), instance );
        }

        [Fact]
        public void requires_settings()
        {
            settings = null!;
            Assert.Throws<ArgumentNullException>( nameof(settings), instance );
        }
    }

    public abstract class Execute : HandlerTests
    {
        InvokeAction command = new();
        Task method() => instance().Execute( command, default );

        readonly Fixture _fixture = new();

        [Fact]
        public async Task requires_command()
        {
            command = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(command), method );
        }

        public class WhenTenantSpecified : Execute
        {
            public WhenTenantSpecified()
            {
                command = _fixture.Create<InvokeAction>();
            }

            [Fact]
            public async Task executes_located_action_directly()
            {
                var factory = new Mock<IActionFactory>( MockBehavior.Strict );
                container.Setup( _ => _.GetInstance<IActionFactory>( command.Action ) ).Returns( factory.Object );

                var dispatcher = new Mock<ICommandDispatcher>( MockBehavior.Strict );
                container.Setup( _ => _.GetInstance<ICommandDispatcher>() ).Returns( dispatcher.Object );

                var contexts = new List<ActionContext>();
                factory.Setup( _ => _.Create( Capture.In( contexts ), default ) ).ReturnsAsync( new FakeAction() );

                var executed = new List<Command>();
                dispatcher.Setup( _ => _.Execute( Capture.In( executed ), default ) ).ReturnsAsync( ValueTuple.Create() );

                await method();
                contexts.Should().ContainSingle().Subject.Should().BeSameAs( command.Context );
                executed.Should().ContainSingle().Subject.Should().BeOfType<FakeAction>();
            }
        }

        public class WhenTenantNotSpecified : Execute
        {
            public WhenTenantNotSpecified()
            {
                command = _fixture.Create<InvokeAction>();
            }

            [Theory]
            [InlineData( null )]
            [WhitespaceCases]
            public async Task executes_expanded_contexts( string tenant )
            {
                // clear tenant from context
                command = command with { Context = command.Context with { Tenant = tenant! } };

                var factory = new Mock<IActionFactory>( MockBehavior.Strict );
                container.Setup( _ => _.GetInstance<IActionFactory>( command.Action ) ).Returns( factory.Object );

                var dispatcher = new Mock<ICommandDispatcher>( MockBehavior.Strict );
                container.Setup( _ => _.GetInstance<ICommandDispatcher>() ).Returns( dispatcher.Object );

                var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
                settings.Setup( _ => _.For( command.Action, command.Context ) ).Returns( configuration );

                var expectedActions = new List<FakeAction>();
                var expectedContexts = new List<ActionContext>();
                var actualActions = new List<Command>();

                // add configurations to execute
                for ( var i = 0; i < 3; i++ )
                {
                    var expectedTenant = _fixture.Create<string>();
                    var configs = _fixture.CreateMany<string>( i ).ToArray();

                    // when no configs, add an empty entry
                    // this should cause the tenant to be executed with its own configuration
                    if ( !configs.Any() )
                    {
                        configuration[$"tenants:{expectedTenant}"] = null;
                        expectedContexts.Add( new() { Tenant = expectedTenant, Configuration = string.Empty } );
                    }

                    // for those with configs, iterate them and add to the configuration collection
                    else
                    {
                        for ( var j = 0; j < configs.Length; j++ )
                        {
                            configuration[$"tenants:{expectedTenant}:{j}"] = configs[j];
                            expectedContexts.Add( new() { Tenant = expectedTenant, Configuration = configs[j] } );
                        }
                    }
                }

                foreach ( var context in expectedContexts )
                {
                    var actualContext = context;
                    var configuredAction = new FakeAction();
                    factory.Setup( _ => _.Create( actualContext, default ) ).ReturnsAsync( configuredAction );
                    expectedActions.Add( configuredAction );
                }

                dispatcher.Setup( _ => _.Execute( Capture.In( actualActions ), default ) ).ReturnsAsync( ValueTuple.Create() );

                await method();
                actualActions.Should().AllBeOfType<FakeAction>().Subject.Should().BeEquivalentTo( expectedActions );
            }
        }
    }
}
