// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

using System.Collections;

namespace Shipwright.Dataflows.Transformations.DbUpsertTests;

public class HandlerTests
{
    readonly Fixture fixture = new Fixture().WithDataflowCustomization();
    Mock<DbUpsert.Handler> mock;
    Mock<DbUpsert.Handler> instance() => new( MockBehavior.Default ) { CallBase = true };

    public HandlerTests()
    {
        mock = instance();
    }

    public class Constructor : HandlerTests
    {
        ITransformationHandler constructor() => new DbUpsert.Handler();
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
}
