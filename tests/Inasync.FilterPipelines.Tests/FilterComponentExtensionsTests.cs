using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class FilterComponentExtensionsTests {

        [TestMethod]
        public void ToMiddleware_Null() {
            new TestCaseRunner()
                .Run(() => FilterComponentExtensions.ToMiddleware((SpyFilterComponent)null))
                .Verify((actual, desc) => { }, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void ToMiddleware() {
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> middlewareResult = _ => Task.FromResult<FilterFunc<DummyEntity>>(__ => Rand.Array<DummyEntity>());
            var component = new SpyFilterComponent(middlewareResult);
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = _ => Task.FromResult<FilterFunc<DummyEntity>>(__ => Rand.Array<DummyEntity>());

            new TestCaseRunner()
                .Run(() => FilterComponentExtensions.ToMiddleware(component)(next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(middlewareResult, actual, desc);

                    Assert.AreEqual(next, component.ActualNext, desc);
                }, (Type)null);
        }

        #region Helpers

        private sealed class SpyFilterComponent : IFilterComponent<DummyContext, DummyEntity> {
            private readonly Func<DummyContext, Task<FilterFunc<DummyEntity>>> _result;

            public SpyFilterComponent(Func<DummyContext, Task<FilterFunc<DummyEntity>>> result) => _result = result;

            public Func<DummyContext, Task<FilterFunc<DummyEntity>>> ActualNext { get; private set; }

            public Func<DummyContext, Task<FilterFunc<DummyEntity>>> Middleware(Func<DummyContext, Task<FilterFunc<DummyEntity>>> next) {
                ActualNext = next;
                return _result;
            }
        }

        #endregion Helpers
    }
}
