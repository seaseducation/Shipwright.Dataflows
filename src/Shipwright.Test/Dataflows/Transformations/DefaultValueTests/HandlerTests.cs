// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture.Xunit2;
using FluentAssertions;
using YamlDotNet.Core.Tokens;

namespace Shipwright.Dataflows.Transformations.DefaultValueTests;

public class HandlerTests
{
    DefaultValue transformation = new Fixture().Create<DefaultValue>();
    ITransformationHandler instance() => new DefaultValue.Handler( transformation );

    public class Constructor : HandlerTests
    {
        [Fact]
        public void requires_transformation()
        {
            transformation = null!;
            Assert.Throws<ArgumentNullException>( nameof(transformation), instance );
        }
    }

    public abstract class Transform : HandlerTests
    {
        Record record = new Fixture().WithDataflowCustomization().Create<Record>();
        Task method() => instance().Transform( record, default );

        [Fact]
        public async Task requires_record()
        {
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        [Fact]
        public async Task defaults_on_missing_value()
        {
            var fixture = new Fixture();
            var expected = new Dictionary<string, object?>( record.Data );

            for ( var i = 0; i < 3; i++ )
                expected[fixture.Create<string>()] = Guid.NewGuid();

            transformation.Defaults.Clear();
            foreach ( var (field, value) in expected )
            {
                transformation.Defaults.Add( new( field, () => value ) );
                record.Data.Remove( field );
            }

            await method();
            record.Data.Should().BeEquivalentTo( expected );
        }

        [Fact]
        public async Task defaults_on_null_value()
        {
            var fixture = new Fixture();
            var expected = new Dictionary<string, object?>( record.Data );

            for ( var i = 0; i < 3; i++ )
                expected[fixture.Create<string>()] = Guid.NewGuid();

            transformation.Defaults.Clear();
            foreach ( var (field, value) in expected )
            {
                transformation.Defaults.Add( new( field, () => value ) );
                record.Data[field] = null;
            }

            await method();
            record.Data.Should().BeEquivalentTo( expected );
        }

        [Theory]
        [AutoData]
        public async Task does_not_default_on_given_string_values( string value )
        {
            var fixture = new Fixture();
            var fields = fixture.CreateMany<string>();

            transformation.Defaults.Clear();
            foreach ( var field in fields )
            {
                record[field] = value;
                transformation.Defaults.Add( new( field, () => Guid.NewGuid() ) );
            }

            var expected = new Dictionary<string, object?>( record.Data );
            await method();

            record.Data.Should().BeEquivalentTo( expected );
        }

        [Theory]
        [AutoData]
        public async Task does_not_default_on_non_string_values( Guid value )
        {
            var fixture = new Fixture();
            var fields = fixture.CreateMany<string>();

            transformation.Defaults.Clear();
            foreach ( var field in fields )
            {
                record[field] = value;
                transformation.Defaults.Add( new( field, () => Guid.NewGuid() ) );
            }

            var expected = new Dictionary<string, object?>( record.Data );
            await method();

            record.Data.Should().BeEquivalentTo( expected );
        }

        public class WhenDefaultOnBlankFalse : Transform
        {
            public WhenDefaultOnBlankFalse() => transformation = transformation with { DefaultOnBlank = false };

            [Theory]
            [WhitespaceCases]
            [AutoData]
            public async Task does_not_default_on_whitespace_nor_string_values( string value )
            {
                var fixture = new Fixture();
                var fields = fixture.CreateMany<string>();

                transformation.Defaults.Clear();
                foreach ( var field in fields )
                {
                    record[field] = value;
                    transformation.Defaults.Add( new( field, () => Guid.NewGuid() ) );
                }

                var expected = new Dictionary<string, object?>( record.Data );
                await method();

                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenDefaultOnBlankTrue : Transform
        {
            [Theory]
            [WhitespaceCases]
            public async Task defaults_on_whitespace_value( string whitespace )
            {
                var fixture = new Fixture();
                var expected = new Dictionary<string, object?>( record.Data );

                for ( var i = 0; i < 3; i++ )
                    expected[fixture.Create<string>()] = Guid.NewGuid();

                transformation.Defaults.Clear();
                foreach ( var (field, value) in expected )
                {
                    transformation.Defaults.Add( new( field, () => value ) );
                    record.Data[field] = whitespace;
                }

                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }
    }
}
