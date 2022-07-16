// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using AutoFixture;
using FluentAssertions;
using Shipwright.Commands;
using Shipwright.Dataflows.Sources;
using Shipwright.Dataflows.Transformations;
using System.Threading.Tasks.Dataflow;

namespace Shipwright.Dataflows.DataflowTests;

public class HandlerTests
{
    Mock<ISourceReaderFactory> readerFactory = new( MockBehavior.Strict );
    Mock<ITransformationHandlerFactory> transformationHandlerFactory = new( MockBehavior.Strict );
    Mock<Dataflow.Helper> helper;
    ICommandHandler<Dataflow> instance() => new Dataflow.Handler( helper?.Object! );

    protected HandlerTests()
    {
        helper = new( MockBehavior.Strict, readerFactory.Object, transformationHandlerFactory.Object );
    }

    public class Constructor : HandlerTests
    {
        [Fact]
        public void requires_helper()
        {
            helper = null!;
            Assert.Throws<ArgumentNullException>( nameof(helper), instance );
        }
    }

    public abstract class Execute : HandlerTests
    {
        readonly Fixture _fixture = new Fixture().WithDataflowCustomization();
        Dataflow command;
        CancellationToken cancellationToken;
        Task method() => instance().Execute( command, cancellationToken );

        public Execute()
        {
            command = _fixture.Create<Dataflow>();
        }

        [Fact]
        public async Task requires_command()
        {
            command = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(command), method );
        }

        public class WhenNotCanceled : Execute
        {
            public WhenNotCanceled()
            {
                cancellationToken = new( false );
            }

            [Fact]
            public async Task configures_and_executes_dataflow()
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
                helper.Setup( _ => _.CreateLinkedTokenSource( cancellationToken ) ).Returns( cts );
                var linkedToken = cts.Token;

                var reader = new Mock<ISourceReader>( MockBehavior.Strict );
                helper.Setup( _ => _.GetSourceReader( command, linkedToken ) ).ReturnsAsync( reader.Object );
                var transformationHandler = new Mock<ITransformationHandler>( MockBehavior.Strict );
                helper.Setup( _ => _.GetTransformationHandler( command, linkedToken ) ).ReturnsAsync( transformationHandler.Object );

                var blockOptions = new ExecutionDataflowBlockOptions { CancellationToken = linkedToken };
                helper.Setup( _ => _.GetDataflowBlockOptions( command, linkedToken ) ).Returns( blockOptions );

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                helper.Setup( _ => _.GetDataflowLinkOptions() ).Returns( linkOptions );

                var expected = _fixture.CreateMany<Record>().ToArray();
                reader.Setup( _ => _.Read( linkedToken ) ).Returns( expected.ToAsyncEnumerable() );

                var actual = new List<Record>();
                transformationHandler.Setup( _ => _.Transform( Capture.In( actual ), linkedToken ) ).Returns( Task.CompletedTask );
                transformationHandler.Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );

                await method();
                actual.Should().BeEquivalentTo( expected );
            }
        }

        public class WhenCanceledBeforeExecution : Execute
        {
            public WhenCanceledBeforeExecution()
            {
                cancellationToken = new( true );
            }

            [Fact]
            public async Task aborts_dataflow()
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
                helper.Setup( _ => _.CreateLinkedTokenSource( cancellationToken ) ).Returns( cts );
                var linkedToken = cts.Token;

                var reader = new Mock<ISourceReader>( MockBehavior.Strict );
                helper.Setup( _ => _.GetSourceReader( command, linkedToken ) ).ReturnsAsync( reader.Object );
                var transformationHandler = new Mock<ITransformationHandler>( MockBehavior.Strict );
                helper.Setup( _ => _.GetTransformationHandler( command, linkedToken ) ).ReturnsAsync( transformationHandler.Object );

                var blockOptions = new ExecutionDataflowBlockOptions { CancellationToken = linkedToken };
                helper.Setup( _ => _.GetDataflowBlockOptions( command, linkedToken ) ).Returns( blockOptions );

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                helper.Setup( _ => _.GetDataflowLinkOptions() ).Returns( linkOptions );

                var records = _fixture.CreateMany<Record>().ToArray();
                reader.Setup( _ => _.Read( linkedToken ) ).Returns( records.ToAsyncEnumerable() );

                transformationHandler.Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );

                await Assert.ThrowsAsync<TaskCanceledException>( method );
                cts.IsCancellationRequested.Should().BeTrue();
            }
        }

        public class WhenCanceledDuringDataflow : Execute
        {
            public WhenCanceledDuringDataflow()
            {
                cancellationToken = new( false );
            }

            [Fact]
            public async Task aborts_dataflow()
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
                helper.Setup( _ => _.CreateLinkedTokenSource( cancellationToken ) ).Returns( cts );
                var linkedToken = cts.Token;

                var reader = new Mock<ISourceReader>( MockBehavior.Strict );
                helper.Setup( _ => _.GetSourceReader( command, linkedToken ) ).ReturnsAsync( reader.Object );
                var transformationHandler = new Mock<ITransformationHandler>( MockBehavior.Strict );
                helper.Setup( _ => _.GetTransformationHandler( command, linkedToken ) ).ReturnsAsync( transformationHandler.Object );

                var blockOptions = new ExecutionDataflowBlockOptions { CancellationToken = linkedToken };
                helper.Setup( _ => _.GetDataflowBlockOptions( command, linkedToken ) ).Returns( blockOptions );

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                helper.Setup( _ => _.GetDataflowLinkOptions() ).Returns( linkOptions );

                var records = _fixture.CreateMany<Record>().ToArray();
                reader.Setup( _ => _.Read( linkedToken ) ).Returns( records.ToAsyncEnumerable() );

                transformationHandler.Setup( _ => _.Transform( It.IsAny<Record>(), linkedToken ) )
                    .Returns( Task.CompletedTask )
                    .Callback( () => cts.Cancel() );

                transformationHandler.Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );

                await Assert.ThrowsAsync<TaskCanceledException>( method );
                cts.IsCancellationRequested.Should().BeTrue();
            }
        }

        public class WhenCancellationExceptionThrown : Execute
        {
            public WhenCancellationExceptionThrown()
            {
                cancellationToken = new( false );
            }

            [Fact]
            public async Task aborts_dataflow()
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource( cancellationToken );
                helper.Setup( _ => _.CreateLinkedTokenSource( cancellationToken ) ).Returns( cts );
                var linkedToken = cts.Token;

                var reader = new Mock<ISourceReader>( MockBehavior.Strict );
                helper.Setup( _ => _.GetSourceReader( command, linkedToken ) ).ReturnsAsync( reader.Object );
                var transformationHandler = new Mock<ITransformationHandler>( MockBehavior.Strict );
                helper.Setup( _ => _.GetTransformationHandler( command, linkedToken ) ).ReturnsAsync( transformationHandler.Object );

                var blockOptions = new ExecutionDataflowBlockOptions { CancellationToken = linkedToken };
                helper.Setup( _ => _.GetDataflowBlockOptions( command, linkedToken ) ).Returns( blockOptions );

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                helper.Setup( _ => _.GetDataflowLinkOptions() ).Returns( linkOptions );

                var records = _fixture.CreateMany<Record>().ToArray();
                reader.Setup( _ => _.Read( linkedToken ) ).Returns( records.ToAsyncEnumerable() );

                transformationHandler.Setup( _ => _.Transform( It.IsAny<Record>(), linkedToken ) )
                    .ThrowsAsync( new OperationCanceledException() );

                transformationHandler.Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );

                await Assert.ThrowsAsync<TaskCanceledException>( method );
                cts.IsCancellationRequested.Should().BeTrue();
            }
        }
    }
}
