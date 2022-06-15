// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shipwright.Dataflows.EventSinks;

namespace Shipwright.Dataflows.Sources.CsvSourceTests;

public class ReaderTests
{
    CsvSource source = new() { Description = Guid.NewGuid().ToString() };
    Dataflow dataflow = new();
    ILogger<CsvSource> logger = new NullLogger<CsvSource>();
    ISourceReader instance() => new CsvSource.Reader( source, dataflow, logger );

    public class Constructor : ReaderTests
    {
        [Fact]
        public void requires_source()
        {
            source = null!;
            Assert.Throws<ArgumentNullException>( nameof(source), instance );
        }

        [Fact]
        public void requires_dataflow()
        {
            dataflow = null!;
            Assert.Throws<ArgumentNullException>( nameof(dataflow), instance );
        }

        [Fact]
        public void requires_logger()
        {
            logger = null!;
            Assert.Throws<ArgumentNullException>( nameof(logger), instance );
        }
    }

    public abstract class Read : ReaderTests
    {
        Mock<IEventSinkHandler> eventSinkHandler = new( MockBehavior.Strict );
        CancellationToken cancellationToken;
        ValueTask<List<Record>> method() => instance().Read( eventSinkHandler?.Object!, cancellationToken ).ToListAsync();

        [Fact]
        public async Task requires_eventSinkHandler()
        {
            eventSinkHandler = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(eventSinkHandler), () => method().AsTask() );
        }

        public class WhenFileNotFound : Read
        {
            public WhenFileNotFound()
            {
                source = new()
                {
                    Path = $"{Guid.NewGuid()}.csv",
                    Description = $"{Guid.NewGuid()}"
                };
            }

            [Fact]
            public async Task returns_no_results_and_adds_event()
            {
                eventSinkHandler.Setup( _ => _.NotifySourceStarting( source, cancellationToken ) ).Returns( Task.CompletedTask );
                eventSinkHandler.Setup( _ => _.NotifySourceCompleted( source, cancellationToken ) ).Returns( Task.CompletedTask );

                var actual = await method();
                actual.Should().BeEmpty();

                var @event = source.Events.Should().ContainSingle().Subject;
                @event.StopProcessing.Should().BeTrue();
                @event.Level.Should().Be( LogLevel.Critical );
                @event.Description.Should().StartWith( "Could not find file" );
                @event.Value.Should().BeEquivalentTo( new { source.Path } );
            }
        }

        public class WhenFileIsEmpty : Read
        {
            public static IEnumerable<object[]> EmptySourceCases() =>
                from skip in new[] { 0, 1, 3 }
                from hasHeaderRecord in new[] { true, false }
                select new object[] { skip, hasHeaderRecord };

            public WhenFileIsEmpty()
            {
                source = source with { Path = "Dataflows/Sources/CsvSourceTests/EmptyFile.csv" };
            }

            [Theory]
            [MemberData(nameof(EmptySourceCases))]
            // ReSharper disable once ParameterHidesMember
            public async Task returns_no_results( int skip, bool hasHeaderRecord )
            {
                source = source with { Skip = skip };
                source.Configuration.HasHeaderRecord = hasHeaderRecord;

                eventSinkHandler.Setup( _ => _.NotifySourceStarting( source, cancellationToken ) ).Returns( Task.CompletedTask );
                eventSinkHandler.Setup( _ => _.NotifySourceCompleted( source, cancellationToken ) ).Returns( Task.CompletedTask );

                var actual = await method();
                actual.Should().BeEmpty();
                source.Events.Should().BeEmpty();
            }
        }

        public class WhenDuplicateHeaderName : Read
        {
            public WhenDuplicateHeaderName()
            {
                source = source with { Path = "Dataflows/Sources/CsvSourceTests/DuplicateHeader.csv", Skip = 0 };
                source.Configuration.HasHeaderRecord = true;
                source.Configuration.NewLine = "\n";
            }

            [Fact]
            public async Task returns_no_results_and_adds_event()
            {
                eventSinkHandler.Setup( _ => _.NotifySourceStarting( source, cancellationToken ) ).Returns( Task.CompletedTask );
                eventSinkHandler.Setup( _ => _.NotifySourceCompleted( source, cancellationToken ) ).Returns( Task.CompletedTask );

                var actual = await method();
                actual.Should().BeEmpty();

                var e = source.Events.Should().ContainSingle().Subject;
                e.StopProcessing.Should().BeTrue();
                e.Level.Should().Be( LogLevel.Critical );
                e.Description.Should().StartWith( "Duplicate header name: " );
                e.Value.Should().NotBeNull();
            }
        }

        public class WhenFieldCountChangesAfterSkip : Read
        {
            public WhenFieldCountChangesAfterSkip()
            {
                source = source with { Path = "Dataflows/Sources/CsvSourceTests/FieldCountChange.csv" };
                source.Configuration.NewLine = "\n";
            }

            [Fact]
            public async Task returns_no_results_and_adds_event()
            {
                eventSinkHandler.Setup( _ => _.NotifySourceStarting( source, cancellationToken ) ).Returns( Task.CompletedTask );
                eventSinkHandler.Setup( _ => _.NotifySourceCompleted( source, cancellationToken ) ).Returns( Task.CompletedTask );

                var actual = await method();
                actual.Should().BeEmpty();

                var e = source.Events.Should().ContainSingle().Subject;
                e.StopProcessing.Should().BeTrue();
                e.Level.Should().Be( LogLevel.Critical );
                e.Description.Should().StartWith( "An inconsistent number of columns has been detected." );
                e.Value.Should().NotBeNull();
            }
        }

        public class WhenFieldCountChangesBeforeSkip : Read
        {
            public WhenFieldCountChangesBeforeSkip()
            {
                source = source with { Path = "Dataflows/Sources/CsvSourceTests/FieldCountChange.csv", Skip = 3};
                source.Configuration.NewLine = "\n";
                source.Configuration.HasHeaderRecord = false;
            }

            [Fact]
            public async Task returns_records_after_skip()
            {
                eventSinkHandler.Setup( _ => _.NotifySourceStarting( source, cancellationToken ) ).Returns( Task.CompletedTask );
                eventSinkHandler.Setup( _ => _.NotifySourceCompleted( source, cancellationToken ) ).Returns( Task.CompletedTask );

                var actual = await method();
                actual.Count.Should().Be( 2 );
                source.Events.Should().BeEmpty();
            }
        }

        public class WhenUnescapedQuote : Read
        {
            public WhenUnescapedQuote()
            {
                source = source with { Path = "Dataflows/Sources/CsvSourceTests/UnescapedQuote.csv" };
                source.Configuration.NewLine = "\n";
            }

            [Fact]
            public async Task returns_no_records_with_event()
            {
                eventSinkHandler.Setup( _ => _.NotifySourceStarting( source, cancellationToken ) ).Returns( Task.CompletedTask );
                eventSinkHandler.Setup( _ => _.NotifySourceCompleted( source, cancellationToken ) ).Returns( Task.CompletedTask );

                var actual = await method();
                actual.Should().BeEmpty();

                var e = source.Events.Should().ContainSingle().Subject;
                e.StopProcessing.Should().BeTrue();
                e.Level.Should().Be( LogLevel.Critical );
                e.Description.Should().StartWith( "Unescaped quote found on line" );
                e.Value.Should().NotBeNull();
            }
        }

        public class WhenFileIsReadableWithHeader : Read
        {
            public WhenFileIsReadableWithHeader()
            {
                source = source with { Path = "Dataflows/Sources/CsvSourceTests/ValidFile.csv" };
                source.Configuration.NewLine = "\n";
                source.Configuration.HasHeaderRecord = true;
            }

            [Fact]
            public async Task returns_records()
            {
                eventSinkHandler.Setup( _ => _.NotifySourceStarting( source, cancellationToken ) ).Returns( Task.CompletedTask );
                eventSinkHandler.Setup( _ => _.NotifySourceCompleted( source, cancellationToken ) ).Returns( Task.CompletedTask );

                var actual = await method();
                var expected = new List<Record>
                {
                    new( new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase ) { { "A", "x" }, { "B", "y" }, { "C", "z" } }, dataflow, source, 2 ),
                    new( new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase ) { { "A", "1" }, { "B", "2" }, { "C", "\"3\"" } }, dataflow, source, 3 ),
                    new( new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase ) { { "A", "m" }, { "B", null }, { "C", "n" } }, dataflow, source, 4 ),
                    new( new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase ) { { "A", "r" }, { "B", null }, { "C", "t" } }, dataflow, source, 5 ),
                };

                actual.Should().BeEquivalentTo( expected );
                source.Events.Should().BeEmpty();
            }
        }

        public class WhenFileIsReadableWithoutHeader : Read
        {
            public WhenFileIsReadableWithoutHeader()
            {
                source = source with { Path = "Dataflows/Sources/CsvSourceTests/ValidFile.csv" };
                source.Configuration.NewLine = "\n";
                source.Configuration.HasHeaderRecord = false;
            }

            [Fact]
            public async Task returns_records()
            {
                eventSinkHandler.Setup( _ => _.NotifySourceStarting( source, cancellationToken ) ).Returns( Task.CompletedTask );
                eventSinkHandler.Setup( _ => _.NotifySourceCompleted( source, cancellationToken ) ).Returns( Task.CompletedTask );

                var actual = await method();
                var expected = new List<Record>
                {
                    new( new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase ) { { "Field_0", "A" }, { "Field_1", "B" }, { "Field_2", "C" } }, dataflow, source, 1 ),
                    new( new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase ) { { "Field_0", "x" }, { "Field_1", "y" }, { "Field_2", "z" } }, dataflow, source, 2 ),
                    new( new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase ) { { "Field_0", "1" }, { "Field_1", "2" }, { "Field_2", "\"3\"" } }, dataflow, source, 3 ),
                    new( new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase ) { { "Field_0", "m" }, { "Field_1", null }, { "Field_2", "n" } }, dataflow, source, 4 ),
                    new( new Dictionary<string, object?>( StringComparer.OrdinalIgnoreCase ) { { "Field_0", "r" }, { "Field_1", null }, { "Field_2", "t" } }, dataflow, source, 5 ),
                };

                actual.Should().BeEquivalentTo( expected );
                source.Events.Should().BeEmpty();
            }
        }
    }
}
