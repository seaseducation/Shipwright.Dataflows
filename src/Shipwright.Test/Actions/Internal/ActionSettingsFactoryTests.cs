// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Lamar;
using Microsoft.Extensions.Configuration;
using NetEscapades.Configuration.Yaml;

namespace Shipwright.Actions.Internal;

public abstract class ActionSettingsFactoryTests
{
    IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
    Mock<IServiceContext> container = new( MockBehavior.Strict );
    IActionSettingsFactory instance() => new ActionSettingsFactory( configuration, container?.Object! );

    public class Constructor : ActionSettingsFactoryTests
    {
        [Fact]
        public void requires_configuration()
        {
            configuration = null!;
            Assert.Throws<ArgumentNullException>( nameof(configuration), instance );
        }

        [Fact]
        public void requires_container()
        {
            container = null!;
            Assert.Throws<ArgumentNullException>( nameof(container), instance );
        }
    }

    public abstract class For : ActionSettingsFactoryTests
    {
        string action;
        ActionContext context;
        IConfigurationRoot method() => instance().For( action, context );

        readonly Fixture _fixture = new();

        protected For()
        {
            action = _fixture.Create<string>();
            context = _fixture.Create<ActionContext>();
            configuration["base"] = "true";
        }

        [Fact]
        public void returns_base()
        {
            var actual = method();
            actual.Providers.First().Should().BeOfType<ChainedConfigurationProvider>();
        }

        public class WhenNoTenantNorConfig : For
        {
            public WhenNoTenantNorConfig()
            {
                context = new();
            }

            [Fact]
            public void returns_action_defaults()
            {
                var expected = Path.Combine( "Configurations", $"{action}.yml" );
                var actual = method();
                var provider = actual.Providers.Skip( 1 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                provider.Source.Optional.Should().BeTrue();
                provider.Source.Path.Should().Be( expected );
            }
        }

        public abstract class WhenTenant : For
        {
            protected WhenTenant()
            {
                context = new() { Tenant = _fixture.Create<string>() };
            }

            [Fact]
            public void returns_tenant_defaults_after_base()
            {
                var expected = Path.Combine( "Configurations", context.Tenant, "_Default.yml" );
                var actual = method();
                var provider = actual.Providers.Skip( 1 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                provider.Source.Optional.Should().BeTrue();
                provider.Source.Path.Should().Be( expected );
            }

            [Fact]
            public void returns_action_defaults_after_tenant_defaults()
            {
                var expected = Path.Combine( "Configurations", $"{action}.yml" );
                var actual = method();
                var provider = actual.Providers.Skip( 2 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                provider.Source.Optional.Should().BeTrue();
                provider.Source.Path.Should().Be( expected );
            }

            public abstract class WhenNoAlternateConfiguration : WhenTenant
            {
                public class WhenNoParent : WhenNoAlternateConfiguration
                {
                    [Fact]
                    public void returns_tenant_action_after_tenant_defaults()
                    {
                        var expected = Path.Combine( "Configurations", context.Tenant, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 3 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }

                public class WhenCircularParent : WhenNoAlternateConfiguration
                {
                    [Fact]
                    public void throws_invalid_operation()
                    {
                        context = context with { Tenant = "TestTenant" };
                        action = "ActionWithCircularParent";
                        Assert.Throws<InvalidOperationException>( method );
                    }
                }

                public class WhenParent : WhenNoAlternateConfiguration
                {
                    public WhenParent()
                    {
                        context = context with { Tenant = "TestTenant" };
                        action = "ActionWithParent";
                    }

                    [Fact]
                    public void returns_parent_action_after_tenant_defaults()
                    {
                        var expected = Path.Combine( "Configurations", "ParentTenant", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 3 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_tenant_action_after_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", context.Tenant, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 4 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }

                public class WhenParentAndInGroup : WhenNoAlternateConfiguration
                {
                    public WhenParentAndInGroup()
                    {
                        context = context with { Tenant = "TestTenantInGroup" };
                        action = "ActionWithParent";
                    }

                    [Fact]
                    public void returns_parent_action_after_tenant_defaults()
                    {
                        var expected = Path.Combine( "Configurations", "ParentTenant", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 3 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_tenant_action_after_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", "GroupFolder", context.Tenant, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 4 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }

                public class WhenChainedParent : WhenNoAlternateConfiguration
                {
                    public WhenChainedParent()
                    {
                        context = context with { Tenant = "TestTenant" };
                        action = "ActionWithChainedParent";
                    }

                    [Fact]
                    public void returns_chained_parent_action_after_tenant_defaults()
                    {
                        var expected = Path.Combine( "Configurations", "ChainedParent", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 3 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_parent_action_after_chained_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", "ParentTenant", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 4 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_tenant_action_after_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", context.Tenant, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 5 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }
            }

            public abstract class WhenAlternateConfiguration : WhenTenant
            {
                protected WhenAlternateConfiguration()
                {
                    context = context with { Configuration = _fixture.Create<string>() };
                }

                public class WhenCircularParent : WhenAlternateConfiguration
                {
                    [Fact]
                    public void throws_invalid_operation()
                    {
                        context = context with { Configuration = "TestTenant" };
                        action = "ActionWithCircularParent";
                        Assert.Throws<InvalidOperationException>( method );
                    }
                }

                public class WhenNoParent : WhenAlternateConfiguration
                {
                    [Fact]
                    public void returns_alt_action_after_tenant_defaults()
                    {
                        var expected = Path.Combine( "Configurations", context.Configuration!, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 3 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }

                public class WhenParent : WhenAlternateConfiguration
                {
                    public WhenParent()
                    {
                        context = context with { Configuration = "TestTenant" };
                        action = "ActionWithParent";
                    }

                    [Fact]
                    public void returns_parent_action_after_tenant_defaults()
                    {
                        var expected = Path.Combine( "Configurations", "ParentTenant", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 3 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_alt_action_after_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", context.Configuration!, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 4 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }

                public class WhenChainedParent : WhenAlternateConfiguration
                {
                    public WhenChainedParent()
                    {
                        context = context with { Configuration = "TestTenant" };
                        action = "ActionWithChainedParent";
                    }

                    [Fact]
                    public void returns_chained_parent_action_after_tenant_defaults()
                    {
                        var expected = Path.Combine( "Configurations", "ChainedParent", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 3 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_parent_action_after_chained_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", "ParentTenant", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 4 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_alt_action_after_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", context.Configuration!, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 5 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }
            }
        }

        public abstract class WhenNoTenant : For
        {
            protected WhenNoTenant()
            {
                context = context with { Tenant = string.Empty };
            }

            [Fact]
            public void returns_action_defaults_after_base()
            {
                var expected = Path.Combine( "Configurations", $"{action}.yml" );
                var actual = method();
                var provider = actual.Providers.Skip( 1 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                provider.Source.Optional.Should().BeTrue();
                provider.Source.Path.Should().Be( expected );
            }

            public class WhenNoAlternateConfiguration : WhenNoTenant
            {
                public WhenNoAlternateConfiguration()
                {
                    context = context with { Configuration = null };
                }
            }

            public abstract class WhenAlternateConfiguration : WhenNoTenant
            {
                protected WhenAlternateConfiguration()
                {
                    context = context with { Configuration = _fixture.Create<string>() };
                }

                public class WhenCircularParent : WhenAlternateConfiguration
                {
                    [Fact]
                    public void throws_invalid_operation()
                    {
                        context = context with { Configuration = "TestTenant" };
                        action = "ActionWithCircularParent";
                        Assert.Throws<InvalidOperationException>( method );
                    }
                }

                public class WhenNoParent : WhenAlternateConfiguration
                {
                    [Fact]
                    public void returns_alt_action_after_action_defaults()
                    {
                        var expected = Path.Combine( "Configurations", context.Configuration!, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 2 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }

                public class WhenParent : WhenAlternateConfiguration
                {
                    public WhenParent()
                    {
                        context = context with { Configuration = "TestTenant" };
                        action = "ActionWithParent";
                    }

                    [Fact]
                    public void returns_parent_action_after_action_defaults()
                    {
                        var expected = Path.Combine( "Configurations", "ParentTenant", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 2 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_alt_action_after_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", context.Configuration!, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 3 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }

                public class WhenChainedParent : WhenAlternateConfiguration
                {
                    public WhenChainedParent()
                    {
                        context = context with { Configuration = "TestTenant" };
                        action = "ActionWithChainedParent";
                    }

                    [Fact]
                    public void returns_chained_parent_action_after_action_defaults()
                    {
                        var expected = Path.Combine( "Configurations", "ChainedParent", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 2 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_parent_action_after_chained_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", "ParentTenant", $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 3 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }

                    [Fact]
                    public void returns_alt_action_after_parent_action()
                    {
                        var expected = Path.Combine( "Configurations", context.Configuration!, $"{action}.yml" );
                        var actual = method();
                        var provider = actual.Providers.Skip( 4 ).First().Should().BeOfType<YamlConfigurationProvider>().Subject;

                        provider.Source.Optional.Should().BeTrue();
                        provider.Source.Path.Should().Be( expected );
                    }
                }
            }
        }
    }

    public class Create : ActionSettingsFactoryTests
    {
        ActionContext context = new Fixture().Create<ActionContext>();
        new IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        CancellationToken cancellationToken;
        Task<FakeSettings> method() => instance().Create<FakeSettings>( context, configuration, cancellationToken );

        [Fact]
        public async Task requires_context()
        {
            context = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(context), method );
        }

        [Fact]
        public async Task requires_configuration()
        {
            configuration = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(configuration), method );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_settings_from_located_factory( bool canceled )
        {
            cancellationToken = new( canceled );

            var expected = new FakeSettings();
            var factory = new Mock<IActionSettingsFactory<FakeSettings>>( MockBehavior.Strict );
            container.Setup( _ => _.GetInstance( typeof(IActionSettingsFactory<FakeSettings>) ) ).Returns( factory.Object );
            factory.Setup( _ => _.Create( context, configuration, cancellationToken ) ).ReturnsAsync( expected );

            var actual = await method();
            actual.Should().BeSameAs( expected );
        }
    }
}
