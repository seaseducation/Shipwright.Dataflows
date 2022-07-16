// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit.Sdk;

namespace Shipwright.Dataflows.Transformations.ConversionTests;

public class HandlerTests
{
    Conversion transformation = new();
    ITransformationHandler instance() => new Conversion.Handler( transformation );

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

        public class WhenFieldMissing : Transform
        {
            [Fact]
            public async Task no_op()
            {
                transformation = transformation with { Fields = new Fixture().CreateMany<string>().ToList() };
                record.Events.Clear();

                foreach ( var field in transformation.Fields )
                    record.Data.Remove( field );

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Data.Should().BeEquivalentTo( expected );
                record.Events.Should().BeEmpty();
            }
        }

        public class WhenFieldNull : Transform
        {
            [Fact]
            public async Task no_op()
            {
                transformation = transformation with { Fields = new Fixture().CreateMany<string>().ToList() };
                record.Events.Clear();

                foreach ( var field in transformation.Fields )
                    record[field] = null;

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Data.Should().BeEquivalentTo( expected );
                record.Events.Should().BeEmpty();
            }
        }

        public class WhenFieldValueCanConvert : Transform
        {
            [Fact]
            public async Task replaces_with_converted_values()
            {
                var expected = new Dictionary<string, object?>( record.Data );
                var conversions = new Dictionary<object, object?>();
                var fixture = new Fixture();

                transformation = transformation with { Converter = conversions.TryGetValue };

                foreach ( var field in transformation.Fields )
                {
                    var value = fixture.Create<string>();
                    var converted = Guid.NewGuid();
                    record[field] = value;
                    expected[field] = converted;
                    conversions[value] = converted;
                }

                await method();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldValueCannotConvert : Transform
        {
            [Theory]
            [InlineData( true, LogLevel.Error)]
            [InlineData( true, LogLevel.Information)]
            [InlineData( false, LogLevel.Information)]
            public async Task removes_field_and_adds_event( bool fatal, LogLevel logLevel )
            {
                var expectedData = new Dictionary<string, object?>( record.Data );
                var expectedEvents = new List<LogEvent>( record.Events );
                var fixture = new Fixture();

                bool convert( object value, out object? converted )
                {
                    converted = null!;
                    return false;
                }

                var padding = Guid.NewGuid();
                LogEvent onFailure( string field ) => new( fatal, logLevel, $"{padding} {field}" );

                transformation = transformation with
                {
                    Converter = convert,
                    OnFailed = onFailure
                };

                foreach ( var field in transformation.Fields )
                {
                    var value = fixture.Create<string>();
                    record[field] = value;
                    expectedData.Remove( field );
                    expectedEvents.Add( new( fatal, logLevel, $"{padding} {field}", new { value } ) );
                }

                await method();
                record.Data.Should().BeEquivalentTo( expectedData );
                record.Events.Should().BeEquivalentTo( expectedEvents );
            }
        }
    }
}
