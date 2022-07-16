// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture.Xunit2;
using FluentAssertions;
using Shipwright.Dataflows.EventSinks;
using Shipwright.Dataflows.Sources;
using Shipwright.Dataflows.Transformations;
using System.Threading.Tasks.Dataflow;

namespace Shipwright.Dataflows.DataflowTests;

public class HelperTests
{
    Mock<ISourceReaderFactory> readerFactory = new( MockBehavior.Strict );
    Mock<ITransformationHandlerFactory> transformationHandlerFactory = new( MockBehavior.Strict );
    Mock<IEventSinkHandlerFactory> eventSinkHandlerFactory = new( MockBehavior.Strict );
    Dataflow.Helper instance() => new Dataflow.Helper( readerFactory?.Object!, transformationHandlerFactory?.Object!, eventSinkHandlerFactory?.Object! );

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

    public abstract class GetTransformationHandler : HelperTests
    {
        Dataflow dataflow = new Fixture().WithDataflowCustomization().Create<Dataflow>();
        CancellationToken cancellationToken;
        Task<ITransformationHandler> method() => instance().GetTransformationHandler( dataflow, cancellationToken );

        [Fact]
        public async Task requires_dataflow()
        {
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        public class WhenNoKeys : GetTransformationHandler
        {
            public WhenNoKeys()
            {
                dataflow.Keys.Clear();
            }

            [Theory]
            [BooleanCases]
            public async Task returns_aggregate_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                var transformations = new List<Transformation>();
                var expected = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
                transformationHandlerFactory.Setup( _ => _.Create( Capture.In( transformations ), cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                actual.Should().BeSameAs( expected );

                var transformation = transformations.Should().ContainSingle().Subject;
                var aggregate = transformation.Should().BeOfType<AggregateTransformation>().Subject;
                aggregate.Transformations.Should().ContainInOrder( dataflow.Transformations );
                aggregate.Transformations.Should().BeEquivalentTo( dataflow.Transformations );
            }
        }

        public class WhenKeys : GetTransformationHandler
        {
            public WhenKeys()
            {
                dataflow = dataflow with { Keys = new Fixture().CreateMany<string>().ToList() };

            }

            [Theory]
            [BooleanCases]
            public async Task returns_aggregate_handler_with_required_transformation_in_first_position( bool canceled )
            {
                cancellationToken = new( canceled );
                var transformations = new List<Transformation>();
                var expected = new Mock<ITransformationHandler>( MockBehavior.Strict ).Object;
                transformationHandlerFactory.Setup( _ => _.Create( Capture.In( transformations ), cancellationToken ) ).ReturnsAsync( expected );

                var actual = await method();
                actual.Should().BeSameAs( expected );

                var transformation = transformations.Should().ContainSingle().Subject;
                var aggregate = transformation.Should().BeOfType<AggregateTransformation>().Subject;

                // first element should be a required transformation
                var required = aggregate.Transformations.First().Should().BeOfType<Required>().Subject;
                required.Fields.Should().BeEquivalentTo( dataflow.Keys );
                required.AllowEmpty.Should().BeFalse();

                // remaining items should match the dataflow
                aggregate.Transformations.Skip( 1 ).Should().ContainInOrder( dataflow.Transformations );
                aggregate.Transformations.Skip( 1 ).Should().BeEquivalentTo( dataflow.Transformations );
            }
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
