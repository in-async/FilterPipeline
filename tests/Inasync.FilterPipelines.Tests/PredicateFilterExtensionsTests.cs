using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class PredicateFilterExtensionsTests {

        [TestMethod]
        public void ToMiddleware_Null() {
            new TestCaseRunner()
                .Run(() => PredicateFilterExtensions.ToMiddleware((SpyPredicateFilter)null))
                .Verify((actual, desc) => { }, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void ToMiddleware() {
            Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> middlewareResult = _ => Task.FromResult<PredicateFilterFunc<DummyEntity>>(__ => Rand.Bool());
            var filter = new SpyPredicateFilter(middlewareResult);
            Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> next = _ => Task.FromResult<PredicateFilterFunc<DummyEntity>>(__ => Rand.Bool());

            new TestCaseRunner()
                .Run(() => PredicateFilterExtensions.ToMiddleware(filter)(next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(middlewareResult, actual, desc);

                    Assert.AreEqual(next, filter.ActualNext, desc);
                }, (Type)null);
        }

        #region Helpers

        private sealed class SpyPredicateFilter : IPredicateFilter<DummyContext, DummyEntity> {
            private readonly Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> _result;

            public SpyPredicateFilter(Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> result) => _result = result;

            public Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> ActualNext { get; private set; }

            public Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> Middleware(Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> next) {
                ActualNext = next;
                return _result;
            }
        }

        #endregion Helpers
    }
}
