// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using FluentAssertions;

namespace Shipwright.Dataflows.Transformations.CodeTests;

public class HandlerTests
{
    Code transformation = new();
    ITransformationHandler instance() => new Code.Handler( transformation );

    public class Constructor : HandlerTests
    {
        [Fact]
        public void requires_transformation()
        {
            transformation = null!;
            Assert.Throws<ArgumentNullException>( nameof(transformation), instance );
        }
    }

    public class Transform : HandlerTests
    {
        Record record = new Fixture().WithDataflowCustomization().Create<Record>();
        CancellationToken cancellationToken;
        Task method() => instance().Transform( record, cancellationToken );

        [Fact]
        public async Task requires_record()
        {
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        [Theory]
        [BooleanCases]
        public async Task calls_delegate_for_record( bool canceled )
        {
            cancellationToken = new( canceled );

            var capturedRecords = new List<Record>();
            var capturedCancellationTokens = new List<CancellationToken>();

            transformation = transformation with
            {
                Delegate = ( r, ct ) =>
                {
                    capturedRecords.Add( r );
                    capturedCancellationTokens.Add( ct );
                    return Task.CompletedTask;
                }
            };

            await method();
            capturedRecords.Should().ContainSingle().Subject.Should().BeSameAs( record );
            capturedCancellationTokens.Should().ContainSingle().Subject.Should().Be( cancellationToken );
        }
    }
}
