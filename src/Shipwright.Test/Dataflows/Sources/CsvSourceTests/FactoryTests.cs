// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Shipwright.Dataflows.Sources.CsvSourceTests;

public class FactoryTests
{
    ILogger<CsvSource> logger = new NullLogger<CsvSource>();
    ISourceReaderFactory<CsvSource> instance() => new CsvSource.Factory( logger );

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
        CsvSource source = new();
        Dataflow dataflow = new();
        Task<ISourceReader> method() => instance().Create( source, dataflow, default );

        [Fact]
        public async Task requires_source()
        {
            source = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(source), method );
        }

        [Fact]
        public async Task requires_dataflow()
        {
            dataflow = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(dataflow), method );
        }

        [Fact]
        public async Task returns_configured_reader()
        {
            var actual = await method();
            var reader = actual.Should().BeOfType<CsvSource.Reader>().Subject;
            reader._source.Should().Be( source );
            reader._dataflow.Should().Be( dataflow );
            reader._logger.Should().Be( logger );
        }
    }
}
