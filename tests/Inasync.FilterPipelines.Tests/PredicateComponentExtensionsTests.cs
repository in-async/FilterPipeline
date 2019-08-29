using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class PredicateComponentExtensionsTests {

        [TestMethod]
        public void ToMiddleware_Null() {
            new TestCaseRunner()
                .Run(() => PredicateComponentExtensions.ToMiddleware((SpyPredicateComponent)null))
                .Verify((actual, desc) => { }, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void ToMiddleware() {
            Func<DummyContext, Task<PredicateFunc<DummyEntity>>> middlewareResult = _ => Task.FromResult<PredicateFunc<DummyEntity>>(__ => Rand.Bool());
            var component = new SpyPredicateComponent(middlewareResult);
            Func<DummyContext, Task<PredicateFunc<DummyEntity>>> next = _ => Task.FromResult<PredicateFunc<DummyEntity>>(__ => Rand.Bool());

            new TestCaseRunner()
                .Run(() => PredicateComponentExtensions.ToMiddleware(component)(next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(middlewareResult, actual, desc);

                    Assert.AreEqual(next, component.ActualNext, desc);
                }, (Type)null);
        }

        #region Helpers

        private sealed class SpyPredicateComponent : IPredicateComponent<DummyContext, DummyEntity> {
            private readonly Func<DummyContext, Task<PredicateFunc<DummyEntity>>> _result;

            public SpyPredicateComponent(Func<DummyContext, Task<PredicateFunc<DummyEntity>>> result) => _result = result;

            public Func<DummyContext, Task<PredicateFunc<DummyEntity>>> ActualNext { get; private set; }

            public Func<DummyContext, Task<PredicateFunc<DummyEntity>>> Middleware(Func<DummyContext, Task<PredicateFunc<DummyEntity>>> next) {
                ActualNext = next;
                return _result;
            }
        }

        #endregion Helpers
    }
}
