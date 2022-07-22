// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using Identifiable;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Shipwright.Databases;
using SqlKata.Compilers;

namespace Shipwright.Dataflows.Transformations.DbUpsertTests;

public class HandlerTests
{
    readonly Fixture fixture = new Fixture().WithDataflowCustomization();
    DbUpsert transformation;
    Mock<IDbConnectionFactory> connectionFactory = new( MockBehavior.Strict );
    Mock<Compiler> compiler = new( MockBehavior.Strict );
    Mock<DbUpsert.Handler> mock;
    Mock<ITransformationHandler> beforeInsertHandler = new( MockBehavior.Strict );
    Mock<ITransformationHandler> afterInsertHandler = new( MockBehavior.Strict );
    Mock<ITransformationHandler> beforeUpdateHandler = new( MockBehavior.Strict );
    Mock<ITransformationHandler> afterUpdateHandler = new( MockBehavior.Strict );
    Mock<DbUpsert.Handler> instance() => new( MockBehavior.Default,
        transformation,
        connectionFactory.Object,
        compiler.Object,
        beforeInsertHandler?.Object,
        afterInsertHandler?.Object,
        beforeUpdateHandler?.Object,
        afterUpdateHandler?.Object
    ) { CallBase = true };

    protected HandlerTests()
    {
        transformation = fixture.Create<DbUpsert>();
        mock = instance();
    }

    public class Constructor : HandlerTests
    {
        ITransformationHandler constructor() => new DbUpsert.Handler
        (
            transformation,
            connectionFactory?.Object!,
            compiler?.Object!,
            beforeInsertHandler?.Object!,
            afterInsertHandler?.Object!,
            beforeUpdateHandler?.Object!,
            afterUpdateHandler?.Object!
        );

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
                mock.InSequence( sequence ).Setup( _ => _.BeforeInsert( record, cancellationToken ) ).Returns( Task.CompletedTask );
                mock.InSequence( sequence ).Setup( _ => _.Insert( record, cancellationToken ) ).Returns( Task.CompletedTask );
                mock.InSequence( sequence ).Setup( _ => _.AfterInsert( record, cancellationToken ) ).Returns( Task.CompletedTask );
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
                mock.InSequence( sequence ).Setup( _ => _.BeforeUpdate( record, cancellationToken ) ).Returns( Task.CompletedTask );
                mock.InSequence( sequence ).Setup( _ => _.Update( record, changes, cancellationToken ) ).Returns( Task.CompletedTask );
                mock.InSequence( sequence ).Setup( _ => _.AfterUpdate( record, cancellationToken ) ).Returns( Task.CompletedTask );
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
        readonly Guid id = Guid.NewGuid();
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
        readonly Guid id = Guid.NewGuid();
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
        readonly Record record;
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
        readonly Record record;
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
        readonly Record record;
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

    public class AreEqual : HandlerTests
    {
        object? incoming;
        object? existing;
        bool method() => mock.Object.AreEqual( incoming, existing );

        public class WhenEquivalent : AreEqual
        {
            [UsedImplicitly]
            public class EquivalentCases : TheoryData<object?, object?>
            {
                public EquivalentCases()
                {
                    // null equivalency
                    Add( null, null );

                    // basic value type equality
                    var guid = Guid.NewGuid();
                    Add( guid, guid );

                    var integer = new Random().Next();
                    Add( integer, integer );

                    // structural equivalency
                    Add( guid.ToByteArray(), guid.ToByteArray().ToArray() );
                    Add( new[] { integer }, new[] { integer } );

                    // boolean/integer equivalency
                    Add( true, 1 );
                    Add( false, 0 );

                    // convertible decimal equivalency
                    Add( Convert.ToDecimal( integer ), integer );
                }
            }

            [Theory]
            [ClassData(typeof(EquivalentCases))]
            public void returns_true( object? inc, object? ex )
            {
                incoming = inc;
                existing = ex;
                method().Should().BeTrue();
            }
        }

        public class WhenNotEquivalent : AreEqual
        {
            [UsedImplicitly]
            public class InequivalentCases : TheoryData<object?, object?>
            {
                public InequivalentCases()
                {
                    // mismatching null values
                    Add( null, Guid.Empty );
                    Add( Guid.Empty, null );

                    // differing value types
                    Add( 0, Guid.Empty );
                    Add( 0, 1 );

                    // differing structural types
                    Add( new[] { 1 }, new[] { 2 } );

                    // differing strings
                    Add( Guid.NewGuid().ToString(), Guid.Empty.ToString() );

                    // differing types
                    Add( Guid.Empty, Guid.Empty.ToString() );
                }
            }

            [Theory]
            [ClassData(typeof(InequivalentCases))]
            public void returns_false( object? inc, object? ex )
            {
                incoming = inc;
                existing = ex;
                method().Should().BeFalse();
            }
        }
    }

    public abstract class TryGetChanges : HandlerTests
    {
        readonly Record record;
        readonly Dictionary<string, object?> existing;
        Dictionary<string, object?> changes;
        bool method() => mock.Object.TryGetChanges( record, existing, out changes );

        DbUpsert.FieldMap.ShouldReplaceDelegate replace = null!;

        protected TryGetChanges()
        {
            record = fixture.Create<Record>();
            existing = new();
            changes = new();
        }

        public static IEnumerable<object[]> NonUpdateTypes = Enum.GetValues<DbUpsert.ColumnType>()
            .Where( type => type != DbUpsert.ColumnType.Update )
            .Select( type => new object[] { type } )
            .ToArray();

        [Theory]
        [MemberData(nameof(NonUpdateTypes))]
        public void ignores_differences_in_non_update_fields( DbUpsert.ColumnType type )
        {
            record.Data.Clear();
            var (field, column) = fixture.Create<(string, string)>();
            transformation.Fields.Add( new( type, field, column ) { Replace = replace } );
            record[field] = fixture.Create<string>();
            existing[column] = fixture.Create<string>();

            method().Should().BeFalse();
            changes.Should().BeEmpty();
        }

        public class WhenReplaceIsTrue : TryGetChanges
        {
            public WhenReplaceIsTrue()
            {
                replace = ( _, _ ) => true;
            }

            [Fact]
            public void detects_differences_in_update_fields()
            {
                record.Data.Clear();
                var (field, column) = fixture.Create<(string, string)>();
                existing[column] = fixture.Create<string>();
                var expected = record[field] = fixture.Create<string>();

                replace = ( item1, item2 ) =>
                {
                    item1.Should().Be( expected );
                    item2.Should().Be( existing[column] );
                    return true;
                };

                transformation.Fields.Add( new( DbUpsert.ColumnType.Update, field, column ) { Replace = replace } );

                method().Should().BeTrue();
                changes.Should().ContainSingle();
                changes[column].Should().Be( expected );
            }
        }

        public class WhenReplaceIsFalse : TryGetChanges
        {
            public WhenReplaceIsFalse()
            {
                replace = ( _, _ ) => false;
            }

            [Fact]
            public void ignores_differences_in_update_fields()
            {
                record.Data.Clear();
                var (field, column) = fixture.Create<(string, string)>();
                existing[column] = fixture.Create<string>();
                var expected = record[field] = fixture.Create<string>();

                replace = ( item1, item2 ) =>
                {
                    item1.Should().Be( expected );
                    item2.Should().Be( existing[column] );
                    return false;
                };

                transformation.Fields.Add( new( DbUpsert.ColumnType.Update, field, column ) { Replace = replace } );

                method().Should().BeFalse();
                changes.Should().BeEmpty();
            }
        }
    }

    public class Insert : HandlerTests
    {
        readonly Record record;
        CancellationToken cancellationToken;
        Task method() => mock.Object.Insert( record, cancellationToken );

        public Insert()
        {
            record = fixture.Create<Record>();
        }

        [Theory]
        [BooleanCases]
        public async Task inserts_all_mapped_column_values( bool canceled )
        {
            cancellationToken = new( canceled );
            var expected = new Dictionary<string, object?>();

            foreach ( var (_, field, column) in transformation.Fields )
                expected[column] = record[field] = fixture.Create<string>();

            var actual = new List<Dictionary<string, object?>>();
            mock.Setup( _ => _.Insert( Capture.In( actual ), cancellationToken ) ).Returns( Task.CompletedTask );

            await method();
            actual.Should().ContainSingle().Subject.Should().BeEquivalentTo( expected );
        }
    }

    public class BeforeInsert : HandlerTests
    {
        readonly Record record;
        CancellationToken cancellationToken;
        Task method() => mock.Object.BeforeInsert( record, cancellationToken );

        protected BeforeInsert()
        {
            record = fixture.Create<Record>();
        }

        public class WhenDefined : BeforeInsert
        {
            [Theory]
            [BooleanCases]
            public async Task calls_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                beforeInsertHandler.Setup( _ => _.Transform( record, cancellationToken ) ).Returns( Task.CompletedTask );
                await method();
                beforeInsertHandler.Verify( _ => _.Transform( record, cancellationToken ), Times.Once() );
            }
        }

        public class WhenNotDefined : BeforeInsert
        {
            [Theory]
            [BooleanCases]
            public async Task does_not_call_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                beforeInsertHandler = null!;
                mock = instance();
                await method();
            }
        }
    }

    public class AfterInsert : HandlerTests
    {
        readonly Record record;
        CancellationToken cancellationToken;
        Task method() => mock.Object.AfterInsert( record, cancellationToken );

        protected AfterInsert()
        {
            record = fixture.Create<Record>();
        }

        public class WhenDefined : AfterInsert
        {
            [Theory]
            [BooleanCases]
            public async Task calls_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                afterInsertHandler.Setup( _ => _.Transform( record, cancellationToken ) ).Returns( Task.CompletedTask );
                await method();
                afterInsertHandler.Verify( _ => _.Transform( record, cancellationToken ), Times.Once() );
            }
        }

        public class WhenNotDefined : AfterInsert
        {
            [Theory]
            [BooleanCases]
            public async Task does_not_call_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                afterInsertHandler = null!;
                mock = instance();
                await method();
            }
        }
    }

    public class Update : HandlerTests
    {
        readonly Record record;
        readonly Dictionary<string, object?> changes;
        CancellationToken cancellationToken;
        Task method() => mock.Object.Update( record, changes, cancellationToken );

        public Update()
        {
            record = fixture.Create<Record>();
            changes = fixture.Create<Dictionary<string, object?>>();
        }

        [Theory]
        [BooleanCases]
        public async Task add_triggers_to_changes_and_updates_database( bool canceled )
        {
            cancellationToken = new( canceled );

            // ensure a composite key exists
            for ( var i = 0; i < 2; i++ )
                transformation.Fields.Add( fixture.Create<DbUpsert.FieldMap>() with { Type = DbUpsert.ColumnType.Key } );

            var expectedChanges = new Dictionary<string, object?>( changes );
            var expectedKeys = new Dictionary<string, object?>();

            foreach ( var ( type, field, column ) in transformation.Fields )
            {
                switch ( type )
                {
                    case DbUpsert.ColumnType.Key:
                        expectedKeys[column] = record[field] = fixture.Create<string>();
                        break;
                    case DbUpsert.ColumnType.Trigger:
                        expectedChanges[column] = record[field] = fixture.Create<string>();
                        break;
                }
            }

            var actualKeys = new List<IDictionary<string, object?>>();
            var actualChanges = new List<IDictionary<string, object?>>();
            mock.Setup( _ => _.Update( Capture.In( actualKeys ), Capture.In( actualChanges ), cancellationToken ) ).Returns( Task.CompletedTask );

            await method();
            actualKeys.Should().ContainSingle().Subject.Should().BeEquivalentTo( expectedKeys );
            actualChanges.Should().ContainSingle().Subject.Should().BeEquivalentTo( expectedChanges );
        }
    }

    public class BeforeUpdate : HandlerTests
    {
        readonly Record record;
        CancellationToken cancellationToken;
        Task method() => mock.Object.BeforeUpdate( record, cancellationToken );

        protected BeforeUpdate()
        {
            record = fixture.Create<Record>();
        }

        public class WhenDefined : BeforeUpdate
        {
            [Theory]
            [BooleanCases]
            public async Task calls_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                beforeUpdateHandler.Setup( _ => _.Transform( record, cancellationToken ) ).Returns( Task.CompletedTask );
                await method();
                beforeUpdateHandler.Verify( _ => _.Transform( record, cancellationToken ), Times.Once() );
            }
        }

        public class WhenNotDefined : BeforeUpdate
        {
            [Theory]
            [BooleanCases]
            public async Task does_not_call_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                beforeUpdateHandler = null!;
                mock = instance();
                await method();
            }
        }
    }

    public class AfterUpdate : HandlerTests
    {
        readonly Record record;
        CancellationToken cancellationToken;
        Task method() => mock.Object.AfterUpdate( record, cancellationToken );

        protected AfterUpdate()
        {
            record = fixture.Create<Record>();
        }

        public class WhenDefined : AfterUpdate
        {
            [Theory]
            [BooleanCases]
            public async Task calls_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                afterUpdateHandler.Setup( _ => _.Transform( record, cancellationToken ) ).Returns( Task.CompletedTask );
                await method();
                afterUpdateHandler.Verify( _ => _.Transform( record, cancellationToken ), Times.Once() );
            }
        }

        public class WhenNotDefined : AfterUpdate
        {
            [Theory]
            [BooleanCases]
            public async Task does_not_call_handler( bool canceled )
            {
                cancellationToken = new( canceled );
                afterUpdateHandler = null!;
                mock = instance();
                await method();
            }
        }
    }

    public class DisposeAsync : HandlerTests
    {
        async Task method() => await mock.Object.DisposeAsync();

        public class WhenHasOptionalHandlers : DisposeAsync
        {
            [Fact]
            public async Task disposes_child_handlers()
            {
                var sequence = new MockSequence();
                beforeInsertHandler.InSequence( sequence ).Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );
                afterInsertHandler.InSequence( sequence ).Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );
                beforeUpdateHandler.InSequence( sequence ).Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );
                afterUpdateHandler.InSequence( sequence ).Setup( _ => _.DisposeAsync() ).Returns( ValueTask.CompletedTask );

                await method();
                afterUpdateHandler.Verify( _ => _.DisposeAsync(), Times.Once() );
            }
        }

        public class WhenNoOptionalHandlers : DisposeAsync
        {
            [Fact]
            public async Task no_op()
            {
                beforeInsertHandler = afterInsertHandler = beforeUpdateHandler = afterUpdateHandler = null!;
                mock = instance();
                await method();
            }
        }
    }
}
