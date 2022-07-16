// SPDX-License-Identifier: Proprietary
// Copyright (c) TTCO Holding Company, Inc. and Contributors
// All Rights Reserved.

namespace Shipwright.Dataflows.Transformations.DbLookupTests;

public class CacheHelperDecoratorTests
{
    Mock<DbLookup.IHelper> inner = new( MockBehavior.Strict );
    DbLookup.IHelper instance() => new DbLookup.CacheHelperDecorator( inner?.Object! );

    public class Constructor : CacheHelperDecoratorTests
    {
        [Fact]
        public void requires_inner()
        {
            inner = null!;
            Assert.Throws<ArgumentNullException>( nameof(inner), instance );
        }
    }

    public class GetMatches : CacheHelperDecoratorTests
    {
        [Theory]
        [BooleanCases]
        public async Task returns_cached_results_for_previously_encountered_parameters( bool canceled )
        {
            var cancellationToken = new CancellationToken( canceled );
            var fixture = new Fixture();
            var helper = instance();

            // repeat for a few unique parameter sets
            for ( var i = 0; i < 3; i++ )
            {
                var parameters = fixture.Create<IDictionary<string, object?>>();
                var captured = new List<IDictionary<string,object?>>();

                var expected = new Fixture().CreateMany<IDictionary<string, object?>>().ToArray();

                inner.Setup( _ => _.GetMatches( Capture.In( captured ), cancellationToken ) ).ReturnsAsync( expected );

                // the first round, we should get our unique results from the inner helper
                var actual = await helper.GetMatches( parameters, cancellationToken );
                actual.Should().BeSameAs( expected );
                captured.Should().ContainSingle().Subject.Should().BeSameAs( parameters );

                // the second round, we should get the same results; this time cached
                // we should not have called the inner helper again
                // copy parameters to a new dictionary instance - this ensures the cache is working by value
                parameters = new Dictionary<string, object?>( parameters );
                actual = await helper.GetMatches( parameters, cancellationToken );
                actual.Should().BeSameAs( expected );
                captured.Should().ContainSingle();
            }
        }
    }
}
