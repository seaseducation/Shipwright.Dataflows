// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture.Xunit2;
using CsvHelper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Shipwright.Dataflows.EventSinks;
using Shipwright.Dataflows.Sources;
using Shipwright.Dataflows.Transformations;
using System.Threading.Tasks.Dataflow;
using DefaultValue = Shipwright.Dataflows.Transformations.DefaultValue;

namespace Shipwright.Dataflows.DataflowTests;

public class HelperTests
{
    Mock<ISourceReaderFactory> readerFactory = new( MockBehavior.Strict );
    Mock<ITransformationHandlerFactory> transformationHandlerFactory = new( MockBehavior.Strict );
    Mock<IEventSinkHandlerFactory> eventSinkHandlerFactory = new( MockBehavior.Strict );
    Dataflow.Helper instance() => new( readerFactory?.Object!, transformationHandlerFactory?.Object!, eventSinkHandlerFactory?.Object! );

    public class Constructor : HelperTests
    {
        [Fact]
        public void requires_readerFactory()
        {
            readerFactory = null!;
            Assert.Throws<ArgumentNullException>( nameof(readerFactory), instance );
        }

        [Fact]
        public void requires_transformationHandlerFactory()
        {
            transformationHandlerFactory = null!;
            Assert.Throws<ArgumentNullException>( nameof(transformationHandlerFactory), instance );
        }

        [Fact]
        public void requires_eventSinkHandlerFactory()
        {
            eventSinkHandlerFactory = null!;
            Assert.Throws<ArgumentNullException>( nameof(eventSinkHandlerFactory), instance );
        }
    }

    public class CreateLinkedTokenSource : HelperTests
    {
        CancellationToken cancellationToken;
        CancellationTokenSource method() => instance().CreateLinkedTokenSource( cancellationToken );

        [Fact]
        public void returns_linked_token_source()
        {
            using var cts = new CancellationTokenSource();
            cancellationToken = cts.Token;

            using var actual = method();

            actual.IsCancellationRequested.Should().BeFalse();
            cts.Cancel();
            actual.IsCancellationRequested.Should().BeTrue();
        }
    }

    public class GetDataflowBlockOptions : HelperTests
    {
        Dataflow dataflow = new Fixture().WithDataflowCustomization().Create<Dataflow>();
        CancellationToken cancellationToken;
        ExecutionDataflowBlockOptions method() => instance().GetDataflowBlockOptions( dataflow, cancellationToken );

        [Fact]
        public void requires_dataflow()
        {
            dataflow = null!;
            Assert.Throws<ArgumentNullException>( nameof(dataflow), method );
        }

        [Theory]
        [AutoData]
        public void returns_options_with_matching_maxdop( int maxdop )
        {
            dataflow = dataflow with { MaxDegreeOfParallelism = maxdop };
            var actual = method();
            actual.MaxDegreeOfParallelism.Should().Be( maxdop );
        }

        [Theory]
        [AutoData]
        public void returns_options_with_bounded_capacity_matching_maxdop( int maxdop )
        {
            dataflow = dataflow with { MaxDegreeOfParallelism = maxdop };
            var actual = method();
            actual.BoundedCapacity.Should().Be( maxdop );
        }

        [Fact]
        public void returns_options_with_given_cancellation_token()
        {
            using var cts = new CancellationTokenSource();
            cancellationToken = cts.Token;
            var actual = method();
            actual.CancellationToken.IsCancellationRequested.Should().BeFalse();
            cts.Cancel();
            actual.CancellationToken.IsCancellationRequested.Should().BeTrue();
        }

        [Fact]
        public void returns_otherwise_default_options()
        {
            var expected = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = dataflow.MaxDegreeOfParallelism,
                BoundedCapacity = dataflow.MaxDegreeOfParallelism,
                CancellationToken = cancellationToken,
            };

            var actual = method();
            actual.Should().BeEquivalentTo( expected );
        }
    }

    public class GetDataflowLinkOptions : HelperTests
    {
        DataflowLinkOptions method() => instance().GetDataflowLinkOptions();

        [Fact]
        public void returns_propagate_completion()
        {
            var actual = method();
            actual.PropagateCompletion.Should().BeTrue();
        }

        [Fact]
        public void returns_otherwise_default_options()
        {
            var expected = new DataflowLinkOptions { PropagateCompletion = true };
            var actual = method();
            actual.Should().BeEquivalentTo( expected );
        }
    }

    public class GetSourceReader :  HelperTests
    {
        Dataflow dataflow = new Fixture().WithDataflowCustomization().Create<Dataflow>();
        CancellationToken cancellationToken;
        Task<ISourceReader> method() => instance().GetSourceReader( dataflow, cancellationToken );

        [Fact]
        public async Task requires_dataflow()
        {
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_aggregate_reader( bool canceled )
        {
            cancellationToken = new( canceled );
            var sources = new List<Source>();
            var expected = new Mock<ISourceReader>( MockBehavior.Strict ).Object;
            readerFactory.Setup( _ => _.Create( Capture.In( sources ), dataflow, cancellationToken ) ).ReturnsAsync( expected );

            var actual = await method();
            actual.Should().BeSameAs( expected );

            var source = sources.Should().ContainSingle().Subject;
            var aggregate = source.Should().BeOfType<AggregateSource>().Subject;
            aggregate.Sources.Should().ContainInOrder( dataflow.Sources );
            aggregate.Sources.Should().BeEquivalentTo( dataflow.Sources );
        }
    }

    public abstract class GetKeysRequired : HelperTests
    {
        Dataflow dataflow = new Fixture().WithDataflowCustomization().Create<Dataflow>();
        IEnumerable<Transformation> method() => instance().GetKeysRequired( dataflow ).ToArray();

        [Fact]
        public void requires_dataflow()
        {
            dataflow = null!;
            Assert.Throws<ArgumentNullException>( nameof(dataflow), method );
        }

        public class WhenNoKeys : GetKeysRequired
        {
            [Fact]
            public void yields_no_transformations()
            {
                dataflow.Keys.Clear();
                var actual = method();
                actual.Should().BeEmpty();
            }
        }

        public class WhenKeys : GetKeysRequired
        {
            [Fact]
            public void yields_required_transformation_for_key_fields()
            {
                var actual = method();
                var required = actual.Should().ContainSingle().Subject.Should().BeOfType<Required>().Subject;
                required.Fields.Should().BeEquivalentTo( dataflow.Keys );
            }
        }
    }

    public abstract class GetReplacements : HelperTests
    {
        Dataflow dataflow = new Fixture().WithDataflowCustomization().Create<Dataflow>();
        IEnumerable<Transformation> method() => instance().GetReplacements( dataflow ).ToArray();

        protected GetReplacements()
        {
            dataflow = dataflow with { Configuration = new ConfigurationBuilder().AddInMemoryCollection().Build() };
        }

        [Fact]
        public void requires_dataflow()
        {
            dataflow = null!;
            Assert.Throws<ArgumentNullException>( nameof(dataflow), method );
        }

        public class WhenConfigurationNull : GetReplacements
        {
            [Fact]
            public void yields_no_transformations()
            {
                dataflow = dataflow with { Configuration = null };
                var actual = method();
                actual.Should().BeEmpty();
            }
        }

        public class WhenNoReplacementsConfigured : GetReplacements
        {
            [Fact]
            public void yields_no_transformations()
            {
                var actual = method();
                actual.Should().BeEmpty();
            }
        }

        public class WhenInlineReplacementsConfigured : GetReplacements
        {
            [Fact]
            public void yields_transformation_per_field_with_inline_replacements()
            {
                var fixture = new Fixture();
                var expected = new List<Replace>();
                var fields = fixture.CreateMany<string>();

                foreach ( var field in fields )
                {
                    var replace = new Replace { Fields = { field } };
                    var replacements = fixture.CreateMany<(string incoming, string outgoing)>();

                    foreach ( var (incoming, outgoing) in replacements )
                    {
                        replace.Replacements.Add( new( incoming, outgoing ) );
                        dataflow.Configuration![$"replace:{field}:{incoming}"] = outgoing;
                    }

                    expected.Add( replace );
                }

                var actual = method();
                actual.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenSharedReplacementsConfigured : GetReplacements
        {
            [Fact]
            public void yields_transformation_per_field_with_inline_replacements()
            {
                var fixture = new Fixture();
                var expected = new List<Replace>();
                var fields = fixture.CreateMany<string>();
                var replacementKey = fixture.Create<string>();
                var replacements = fixture.CreateMany<(string incoming, string outgoing)>().ToArray();

                foreach ( var ( incoming, outgoing ) in replacements )
                    dataflow.Configuration![$"{replacementKey}:{incoming}"] = outgoing;

                foreach ( var field in fields )
                {
                    var replace = new Replace { Fields = { field } };

                    foreach ( var (incoming, outgoing) in replacements )
                        replace.Replacements.Add( new( incoming, outgoing ) );

                    dataflow.Configuration![$"replace:{field}"] = replacementKey;
                    expected.Add( replace );
                }

                var actual = method();
                actual.Should().BeEquivalentTo( expected );
            }
        }
    }

    public abstract class GetDefaults : HelperTests
    {
        Dataflow dataflow = new Fixture().WithDataflowCustomization().Create<Dataflow>();
        IEnumerable<Transformation> method() => instance().GetDefaults( dataflow ).ToArray();

        protected GetDefaults()
        {
            dataflow = dataflow with { Configuration = new ConfigurationBuilder().AddInMemoryCollection().Build() };
        }

        [Fact]
        public void requires_dataflow()
        {
            dataflow = null!;
            Assert.Throws<ArgumentNullException>( nameof(dataflow), method );
        }

        public class WhenConfigurationNull : GetDefaults
        {
            [Fact]
            public void yields_no_transformations()
            {
                dataflow = dataflow with { Configuration = null };
                var actual = method();
                actual.Should().BeEmpty();
            }
        }

        public class WhenNoDefaultsConfigured : GetDefaults
        {
            [Fact]
            public void yields_no_transformations()
            {
                var actual = method();
                actual.Should().BeEmpty();
            }
        }

        public class WhenDefaultsConfigured : GetDefaults
        {
            [Fact]
            public void yields_default_value_transformation_for_all_fields()
            {
                var fixture = new Fixture();
                var expected = fixture.CreateMany<(string field, string value)>().ToArray();

                foreach ( var ( field, value ) in expected )
                    dataflow.Configuration![$"default:{field}"] = value;

                var actual = method();
                var transformation = actual.Should().ContainSingle().Subject.Should().BeOfType<DefaultValue>().Subject;
                transformation.DefaultOnBlank.Should().BeTrue();

                var defaults = transformation.Defaults.Select( _ => (field: _.Field, value: _.Value()) ).ToArray();
                defaults.Should().BeEquivalentTo( expected );
            }
        }
    }

    public class GetTransformationHandler : HelperTests
    {
        Dataflow dataflow = new Fixture().WithDataflowCustomization().Create<Dataflow>();
        CancellationToken cancellationToken;
        Task<ITransformationHandler> method() => helper.Object.GetTransformationHandler( dataflow, cancellationToken );

        readonly Mock<Dataflow.Helper> helper;

        public GetTransformationHandler()
        {
            helper = new( MockBehavior.Strict, readerFactory?.Object!, transformationHandlerFactory?.Object!, eventSinkHandlerFactory?.Object! );
        }

        [Fact]
        public async Task requires_dataflow()
        {
            dataflow = null!;
            helper.Setup( _ => _.GetTransformationHandler( dataflow, cancellationToken ) ).CallBase();
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        [Fact]
        public async Task returns_configured_transformations_in_order()
        {
            var fixture = new Fixture();
            var expected = new List<Transformation>();

            // required transformations for keys should be first
            var keys = fixture.CreateMany<FakeTransformation>().ToArray();
            expected.AddRange( keys );
            helper.Setup( _ => _.GetKeysRequired( dataflow ) ).Returns( keys );

            // followed by default values
            var defaults = fixture.CreateMany<FakeTransformation>().ToArray();
            expected.AddRange( defaults );
            helper.Setup( _ => _.GetDefaults( dataflow ) ).Returns( defaults );

            // followed by value replacements
            var replacements = fixture.CreateMany<FakeTransformation>().ToArray();
            expected.AddRange( replacements );
            helper.Setup( _ => _.GetReplacements( dataflow ) ).Returns( replacements );

            // ending with the dataflow transformations
            expected.AddRange( dataflow.Transformations );

            var captured = new List<Transformation>();
            var handler = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
            transformationHandlerFactory.Setup( _ => _.Create( Capture.In( captured ), cancellationToken ) ).ReturnsAsync( handler );

            helper.Setup( _ => _.GetTransformationHandler( dataflow, cancellationToken ) ).CallBase();
            var actual = await method();
            actual.Should().BeSameAs( handler );

            var aggregate = captured.Should().ContainSingle().Subject.Should().BeOfType<AggregateTransformation>().Subject;
            aggregate.Transformations.Should().ContainInOrder( expected );
            aggregate.Transformations.Count.Should().Be( expected.Count );
        }
    }

    public class GetEventSinkHandler : HelperTests
    {
        Dataflow dataflow = new Fixture().WithDataflowCustomization().Create<Dataflow>();
        CancellationToken cancellationToken;
        Task<IEventSinkHandler> method() => instance().GetEventSinkHandler( dataflow, cancellationToken );

        [Fact]
        public async Task requires_dataflow()
        {
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_aggregate_handler( bool canceled )
        {
            cancellationToken = new( canceled );
            var expected = new Mock<IEventSinkHandler>( MockBehavior.Strict ).Object;
            var eventSinks = new List<EventSink>();
            eventSinkHandlerFactory.Setup( _ => _.Create( Capture.In( eventSinks ), dataflow, cancellationToken ) ).ReturnsAsync( expected );

            var actual = await method();
            actual.Should().BeSameAs( expected );

            var eventSink = eventSinks.Should().ContainSingle().Subject;
            var aggregate = eventSink.Should().BeOfType<AggregateEventSink>().Subject;
            aggregate.EventSinks.Should().ContainInOrder( dataflow.EventSinks );
            aggregate.EventSinks.Should().BeEquivalentTo( dataflow.EventSinks );
        }
    }
}
