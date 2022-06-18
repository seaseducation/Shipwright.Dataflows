// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.RequiredTests;

public class HandlerTests
{
    Required transformation = new Required { Fields = new Fixture().CreateMany<string>().ToList() };
    ITransformationHandler instance() => new Required.Handler( transformation );

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
        Record record;
        Task method() => instance().Transform( record, default );

        protected Transform()
        {
            record = new Fixture().WithDataflowCustomization().Create<Record>();
            record.Events.Clear();
        }

        [Fact]
        public async Task requires_record()
        {
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        public class WhenFieldsContainNonString : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task no_events_added_and_data_unchanged( bool allowEmpty )
            {
                transformation = transformation with { AllowEmpty = allowEmpty };
                foreach ( var field in transformation.Fields )
                    record[field] = Guid.NewGuid();

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Events.Should().BeEmpty();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldsContainNonWhitespaceString : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task no_events_added_and_data_unchanged( bool allowEmpty )
            {
                var fixture = new Fixture();
                transformation = transformation with { AllowEmpty = allowEmpty };
                foreach ( var field in transformation.Fields )
                    record[field] = fixture.Create<string>();

                var expected = new Dictionary<string, object?>( record.Data );
                await method();
                record.Events.Should().BeEmpty();
                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldsMissing : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task adds_event_and_data_unchanged( bool allowEmpty )
            {
                transformation = transformation with { AllowEmpty = allowEmpty };

                foreach ( var field in transformation.Fields )
                    record.Data.Remove( field );

                var expected = new Dictionary<string, object?>( record.Data );
                await method();

                foreach ( var field in transformation.Fields )
                    record.Events.Should().Contain( transformation.OnError( field ) );

                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldsContainNull : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task adds_event_and_data_removed( bool allowEmpty )
            {
                var expected = new Dictionary<string, object?>( record.Data );
                transformation = transformation with { AllowEmpty = allowEmpty };

                foreach ( var field in transformation.Fields )
                {
                    record[field] = null;
                    expected.Remove( field );
                }

                await method();

                foreach ( var field in transformation.Fields )
                    record.Events.Should().Contain( transformation.OnError( field ) );

                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenFieldsHaveNullVale : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task adds_event_and_data_removed( bool allowEmpty )
            {
                var expected = new Dictionary<string, object?>( record.Data );
                transformation = transformation with { AllowEmpty = allowEmpty };

                foreach ( var field in transformation.Fields )
                {
                    record[field] = null;
                    expected.Remove( field );
                }

                await method();

                foreach ( var field in transformation.Fields )
                    record.Events.Should().Contain( transformation.OnError( field ) );

                record.Data.Should().BeEquivalentTo( expected );
            }
        }

        public abstract class WhenFieldsContainEmptyOrWhitespaceString : Transform
        {
            public class WhenAllowEmptyTrue : WhenFieldsContainEmptyOrWhitespaceString
            {
                public WhenAllowEmptyTrue() => transformation = transformation with { AllowEmpty = true };

                [Theory]
                [WhitespaceCases]
                public async Task no_events_added_and_data_unchanged( string value )
                {
                    foreach ( var field in transformation.Fields )
                        record[field] = value;

                    var expected = new Dictionary<string, object?>( record.Data );
                    await method();

                    record.Events.Should().BeEmpty();
                    record.Data.Should().BeEquivalentTo( expected );
                }
            }

            public class WhenAllowEmptyFalse : WhenFieldsContainEmptyOrWhitespaceString
            {
                public WhenAllowEmptyFalse() => transformation = transformation with { AllowEmpty = false };

                [Theory]
                [WhitespaceCases]
                public async Task adds_event_and_data_removed( string value )
                {
                    var expected = new Dictionary<string, object?>( record.Data );

                    foreach ( var field in transformation.Fields )
                    {
                        record[field] = value;
                        expected.Remove( field );
                    }

                    await method();

                    foreach ( var field in transformation.Fields )
                        record.Events.Should().Contain( transformation.OnError( field ) );

                    record.Data.Should().BeEquivalentTo( expected );
                }
            }
        }
    }
}
