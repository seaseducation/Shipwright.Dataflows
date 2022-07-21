// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Identifiable;
using Newtonsoft.Json;
using Shipwright.Databases;
using SqlKata.Compilers;
using System.Collections;

namespace Shipwright.Dataflows.Transformations.DbUpsertTests;

public class HandlerTests
{
    readonly Fixture fixture = new Fixture().WithDataflowCustomization();
    DbUpsert transformation;
    Mock<IDbConnectionFactory> connectionFactory = new( MockBehavior.Strict );
    Mock<Compiler> compiler = new( MockBehavior.Strict );
    Mock<DbUpsert.Handler> mock;
    Mock<DbUpsert.Handler> instance() => new( MockBehavior.Default, transformation, connectionFactory.Object, compiler.Object ) { CallBase = true };

    public HandlerTests()
    {
        transformation = fixture.Create<DbUpsert>();
        mock = instance();
    }

    public class Constructor : HandlerTests
    {
        ITransformationHandler constructor() => new DbUpsert.Handler( transformation, connectionFactory?.Object!, compiler?.Object! );

        [Fact]
        public void requires_transformation()
        {
            transformation = null!;
            Assert.Throws<ArgumentNullException>( nameof(transformation), constructor );
        }

        [Fact]
        public void requires_connectionFactory()
        {
            connectionFactory = null!;
            Assert.Throws<ArgumentNullException>( nameof(connectionFactory), constructor );
        }

        [Fact]
        public void requires_compiler()
        {
            compiler = null!;
            Assert.Throws<ArgumentNullException>( nameof(compiler), constructor );
        }
    }

    public abstract class Transform : HandlerTests
    {
        Record record;
        CancellationToken cancellationToken;
        Task method() => mock.Object.Transform( record, cancellationToken );

        protected Transform()
        {
            record = fixture.Create<Record>();
        }

        [Theory]
        [BooleanCases]
        public async Task requires_record( bool canceled )
        {
            cancellationToken = new( canceled );
            record = null!;
            await Assert.ThrowsAsync<ArgumentNullException>( nameof(record), method );
        }

        public class WhenNoExistingRecord : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task performs_insert( bool canceled )
            {
                cancellationToken = new( canceled );

                var sequence = new MockSequence();
                var releaser = new Mock<IDisposable>( MockBehavior.Strict );
                mock.InSequence( sequence ).Setup( _ => _.Lock( record, cancellationToken ) ).ReturnsAsync( releaser.Object );
                mock.InSequence( sequence ).Setup( _ => _.Select( record, cancellationToken ) ).ReturnsAsync( Array.Empty<dynamic>() );
                mock.InSequence( sequence ).Setup( _ => _.Insert( record, cancellationToken ) ).Returns( Task.CompletedTask );
                releaser.InSequence( sequence ).Setup( _ => _.Dispose() );

                await method();
                releaser.Verify( _ => _.Dispose(), Times.Once() );
            }
        }

        public class WhenOneExistingRecordWithChanges : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task performs_update( bool canceled )
            {
                cancellationToken = new( canceled );

                var sequence = new MockSequence();
                var releaser = new Mock<IDisposable>( MockBehavior.Strict );
                var existing = fixture.Create<IDictionary<string, object?>>();
                var changes = fixture.Create<Dictionary<string, object?>>();
                mock.InSequence( sequence ).Setup( _ => _.Lock( record, cancellationToken ) ).ReturnsAsync( releaser.Object );
                mock.InSequence( sequence ).Setup( _ => _.Select( record, cancellationToken ) ).ReturnsAsync( new [] { existing } );
                mock.InSequence( sequence ).Setup( _ => _.TryGetChanges( record, existing, out changes ) ).Returns( true );
                mock.InSequence( sequence ).Setup( _ => _.Update( record, changes, cancellationToken ) ).Returns( Task.CompletedTask );
                releaser.InSequence( sequence ).Setup( _ => _.Dispose() );

                await method();
                releaser.Verify( _ => _.Dispose(), Times.Once() );
            }
        }

        public class WhenOneExistingRecordWithoutChanges : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task does_not_update( bool canceled )
            {
                cancellationToken = new( canceled );

                var sequence = new MockSequence();
                var releaser = new Mock<IDisposable>( MockBehavior.Strict );
                var existing = fixture.Create<IDictionary<string, object?>>();
                var changes = fixture.Create<Dictionary<string, object?>>();
                mock.InSequence( sequence ).Setup( _ => _.Lock( record, cancellationToken ) ).ReturnsAsync( releaser.Object );
                mock.InSequence( sequence ).Setup( _ => _.Select( record, cancellationToken ) ).ReturnsAsync( new [] { existing } );
                mock.InSequence( sequence ).Setup( _ => _.TryGetChanges( record, existing, out changes ) ).Returns( false );
                releaser.InSequence( sequence ).Setup( _ => _.Dispose() );

                await method();
                releaser.Verify( _ => _.Dispose(), Times.Once() );
                mock.Verify( _ => _.Update( record, changes, cancellationToken ), Times.Never() );
            }
        }

        public class WhenMultipleExistingRecords : Transform
        {
            [Theory]
            [BooleanCases]
            public async Task throws_invalid_operation( bool canceled )
            {
                cancellationToken = new( canceled );

                var sequence = new MockSequence();
                var releaser = new Mock<IDisposable>( MockBehavior.Strict );
                var matches = fixture.CreateMany<dynamic>();
                mock.InSequence( sequence ).Setup( _ => _.Lock( record, cancellationToken ) ).ReturnsAsync( releaser.Object );
                mock.InSequence( sequence ).Setup( _ => _.Select( record, cancellationToken ) ).ReturnsAsync( matches );
                releaser.InSequence( sequence ).Setup( _ => _.Dispose() );

                await Assert.ThrowsAsync<InvalidOperationException>( method );
                releaser.Verify( _ => _.Dispose(), Times.Once() );
            }
        }
    }

    public class GetSemaphore : HandlerTests
    {
        Guid id = Guid.NewGuid();
        SemaphoreSlim method() => mock.Object.GetSemaphore( id );

        [Fact]
        public void successive_calls_increment_counter()
        {
            using var semaphore = method();
            mock.Object._semaphores[id].Semaphore.Should().BeSameAs( semaphore );
            mock.Object._semaphores[id].Count.Should().Be( 1 );

            var next = method();
            next.Should().BeSameAs( semaphore );
            mock.Object._semaphores[id].Semaphore.Should().BeSameAs( semaphore );
            mock.Object._semaphores[id].Count.Should().Be( 2 );
        }
    }

    public class ReleaseSemaphore : HandlerTests
    {
        Guid id = Guid.NewGuid();
        void method() => mock.Object.ReleaseSemaphore( id );

        public class WhenLastReference : ReleaseSemaphore
        {
            [Fact]
            public void removes_reference_and_disposes_semaphore()
            {
                var counter = mock.Object._semaphores[id] = new();
                counter.Semaphore.Wait();
                method();
                mock.Object._semaphores.ContainsKey( id ).Should().BeFalse();
                Assert.Throws<ObjectDisposedException>( () => counter.Semaphore.Wait() );
            }
        }

        public class WhenNotLastReference : ReleaseSemaphore
        {
            [Fact]
            public void decrements_counter_and_releases_semaphore()
            {
                var count = 5;
                var counter = mock.Object._semaphores[id] = new() { Count = count };
                counter.Semaphore.Wait();
                method();
                mock.Object._semaphores[id].Should().BeSameAs( counter );
                counter.Count.Should().Be( count - 1 );
                counter.Semaphore.CurrentCount.Should().Be( 1 );
            }
        }
    }

    public class GetRecordIdentifier : HandlerTests
    {
        Record record;
        Guid method() => mock.Object.GetRecordIdentifier( record );

        public GetRecordIdentifier()
        {
            record = fixture.Create<Record>();
        }

        [Fact]
        public void computes_named_identifier_for_key_values()
        {
            var values = new Dictionary<string, object?>();

            // expect empty values for existing keys
            foreach ( var ( type, _, column ) in transformation.Fields.ToArray() )
                if ( type == DbUpsert.ColumnType.Key )
                    values[column] = null;

            // add values for additional keys
            var keys = fixture.CreateMany<(string field, string column)>().ToList();

            foreach ( var ( field, column ) in keys )
            {
                record[field] = values[column] = fixture.Create<string>();
                transformation.Fields.Add( new( DbUpsert.ColumnType.Key, field, column ) );
            }

            var expected = NamedGuid.Compute( NamedGuidAlgorithm.SHA1, Guid.Empty, JsonConvert.SerializeObject( values ).ToUpperInvariant() );
            var actual = method();
            actual.Should().Be( expected );
        }
    }

    public class Lock : HandlerTests
    {
        Record record;
        CancellationToken cancellationToken;
        Task<IDisposable> method() => mock.Object.Lock( record, cancellationToken );

        protected Lock()
        {
            record = fixture.Create<Record>();
        }

        public class WhenCanceled : Lock
        {
            [Fact]
            public async Task throws_task_canceled()
            {
                cancellationToken = new( true );
                await Assert.ThrowsAsync<TaskCanceledException>( method );
            }
        }

        public class WhenNotCanceled : Lock
        {
            public WhenNotCanceled() => cancellationToken = new( false );

            [Fact]
            public async Task returns_disposable_reference_that_releases_semaphore()
            {
                var id = Guid.NewGuid();
                mock.Setup( _ => _.GetRecordIdentifier( record ) ).Returns( id );

                using ( var actual = await method() )
                {
                    actual.Should().BeOfType<DbUpsert.Handler.Releaser>();
                    mock.Object._semaphores[id].Count.Should().Be( 1 );
                    mock.Object._semaphores[id].Semaphore.CurrentCount.Should().Be( 0 );
                }

                mock.Object._semaphores.ContainsKey( id ).Should().BeFalse();
            }

            [Fact]
            public async Task prevents_entry_until_released()
            {
                var id = Guid.NewGuid();
                mock.Setup( _ => _.GetRecordIdentifier( record ) ).Returns( id );

                var first = await method();

                // capture as task
                var next = Task.Run( method, cancellationToken );
                await Task.Delay( 10, cancellationToken );

                // ensure we're actually waiting for entry
                mock.Object._semaphores[id].Count.Should().Be( 2 );
                mock.Object._semaphores[id].Semaphore.CurrentCount.Should().Be( 0 );
                next.IsCompleted.Should().BeFalse();

                // release the first reference
                first.Dispose();

                // second reference should complete
                using var second = await next;
                mock.Object._semaphores[id].Count.Should().Be( 1 );
            }
        }
    }

    public class Select : HandlerTests
    {
        Record record;
        CancellationToken cancellationToken;
        Task<IEnumerable<dynamic>> method() => mock.Object.Select( record, cancellationToken );

        public Select()
        {
            record = fixture.Create<Record>();
        }

        [Theory]
        [BooleanCases]
        public async Task queries_all_columns_with_key_values( bool canceled )
        {
            cancellationToken = new( canceled );

            // ensure a composite key exists
            for ( var i = 0; i < 2; i++ )
                transformation.Fields.Add( fixture.Create<DbUpsert.FieldMap>() with { Type = DbUpsert.ColumnType.Key } );

            var expectedColumns = transformation.Fields.Select( _ => _.Column ).ToArray();
            var expectedParameters = new Dictionary<string, object?>();

            foreach ( var ( type, field, column ) in transformation.Fields )
            {
                record[field] = fixture.Create<string>();

                if ( type == DbUpsert.ColumnType.Key )
                    expectedParameters[column] = record[field];
            }

            var actualColumns = new List<IEnumerable<string>>();
            var actualParameters = new List<IDictionary<string, object?>>();
            var expected = fixture.CreateMany<Dictionary<string,object?>>().ToArray();
            mock.Setup( _ => _.Select( Capture.In( actualColumns ), Capture.In( actualParameters ), cancellationToken ) ).ReturnsAsync( expected );

            var actual = await method();
            actual.Should().BeEquivalentTo( expected );
            actualColumns.Should().ContainSingle().Subject.Should().BeEquivalentTo( expectedColumns );
            actualParameters.Should().ContainSingle().Subject.Should().BeEquivalentTo( expectedParameters );
        }
    }
}
