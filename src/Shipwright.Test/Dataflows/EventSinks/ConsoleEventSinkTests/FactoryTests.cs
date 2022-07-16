// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace Shipwright.Dataflows.EventSinks.ConsoleEventSinkTests;

public class FactoryTests
{
    Mock<ILogger<Dataflow>> logger = new( MockBehavior.Strict );
    IEventSinkHandlerFactory<ConsoleEventSink> instance() => new ConsoleEventSink.Factory( logger?.Object! );

    public class Constructor : FactoryTests
    {
        [Fact]
        public void requires_logger()
        {
            logger = null!;
            Assert.Throws<ArgumentNullException>( nameof(logger), instance );
        }
    }

    public class Create : FactoryTests
    {
        ConsoleEventSink eventSink = new( LogLevel.Warning );
        Dataflow dataflow = new();
        CancellationToken cancellationToken;
        Task<IEventSinkHandler> method() => instance().Create( eventSink, dataflow, cancellationToken );

        [Theory]
        [BooleanCases]
        public async Task requires_eventSink( bool canceled )
        {
            cancellationToken = new( canceled );
            eventSink = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(eventSink), method );
        }

        [Theory]
        [BooleanCases]
        public async Task requires_dataflow( bool canceled )
        {
            cancellationToken = new( canceled );
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        [Theory]
        [BooleanCases]
        public async Task returns_handler( bool canceled )
        {
            cancellationToken = new( canceled );
            var actual = await method();

            var typed = actual.Should().BeOfType<ConsoleEventSink.Handler>().Subject;
            typed._logger.Should().Be( logger.Object );
            typed._dataflow.Should().Be( dataflow );
            typed._eventSink.Should().Be( eventSink );
        }
    }
}
