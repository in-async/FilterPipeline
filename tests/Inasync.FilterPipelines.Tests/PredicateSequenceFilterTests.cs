using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class PredicateSequenceFilterTests {

        [TestMethod]
        public void Ctor() {
            Action TestCase(int testNumber, SpyPredicateFilterCreator predicateCreator, Type expectedExceptionType = null) => () => {
                new TestCaseRunner($"No.{testNumber}")
                    .Run(() => new PredicateSequenceFilter<DummyContext, DummyEntity>(predicateCreator?.Invoke))
                    .Verify((_, __) => { }, expectedExceptionType);
            };

            new[] {
                TestCase( 0, null                                    , typeof(ArgumentNullException)),
                TestCase( 1, new SpyPredicateFilterCreator(_ => true)),
            }.Run();
        }

        [TestMethod]
        public void Middleware() {
            var source = new[] { new DummyEntity(), new DummyEntity() };

            Action TestCase(int testNumber, SpyPredicateFilterCreator predicateCreator, (DummyEntity[] result, DummyEntity[] nextSource) expected) => () => {
                var filter = new PredicateSequenceFilter<DummyContext, DummyEntity>(predicateCreator.Invoke);
                var context = new DummyContext();

                IEnumerable<DummyEntity> actualNextSource = default;
                Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next = ctx => Task.FromResult<SequenceFilterFunc<DummyEntity>>(src => {
                    actualNextSource = src.ToArray();
                    return src;
                });

                new TestCaseRunner($"No.{testNumber}")
                    .Run(async () => (await filter.Middleware(next)(context))(source))
                    .Verify((actual, desc) => {
                        CollectionAssert.AreEqual(expected.result, actual.ToArray(), desc);

                        Assert.AreEqual(context, predicateCreator.ActualContext, desc);
                        CollectionAssert.AreEqual(expected.nextSource, actualNextSource?.ToArray(), desc);
                    }, (Type)null);
            };

            new[] {
                TestCase( 0, new SpyPredicateFilterCreator(_ => false)                                           , (new DummyEntity[0], new DummyEntity[0])),
                TestCase( 1, new SpyPredicateFilterCreator(_ => true )                                           , (source            , source            )),
                TestCase( 2, new SpyPredicateFilterCreator(PredicateFilter<DummyContext, DummyEntity>.NullFilter), (source            , source            )),
            }.Run();
        }

        #region Helpers

        private sealed class SpyPredicateFilterCreator {
            private readonly PredicateFilterFunc<DummyEntity> _predicate;

            public SpyPredicateFilterCreator(PredicateFilterFunc<DummyEntity> predicate) => _predicate = predicate;

            public DummyContext ActualContext { get; private set; }

            public Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> Invoke => context => {
                ActualContext = context;

                return Task.FromResult(_predicate);
            };
        }

        #endregion Helpers
    }
}
