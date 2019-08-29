using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class SequenceFilterExtensionsTests {

        [TestMethod]
        public void ToMiddleware_Null() {
            new TestCaseRunner()
                .Run(() => SequenceFilterExtensions.ToMiddleware((SpySequenceFilter)null))
                .Verify((actual, desc) => { }, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void ToMiddleware() {
            Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> middlewareResult = _ => Task.FromResult<SequenceFilterFunc<DummyEntity>>(__ => Rand.Array<DummyEntity>());
            var filter = new SpySequenceFilter(middlewareResult);
            Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next = _ => Task.FromResult<SequenceFilterFunc<DummyEntity>>(__ => Rand.Array<DummyEntity>());

            new TestCaseRunner()
                .Run(() => SequenceFilterExtensions.ToMiddleware(filter)(next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(middlewareResult, actual, desc);

                    Assert.AreEqual(next, filter.ActualNext, desc);
                }, (Type)null);
        }

        #region Helpers

        private sealed class SpySequenceFilter : ISequenceFilter<DummyEntity, DummyContext> {
            private readonly Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> _result;

            public SpySequenceFilter(Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> result) => _result = result;

            public Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> ActualNext { get; private set; }

            public Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> Middleware(Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next) {
                ActualNext = next;
                return _result;
            }
        }

        #endregion Helpers
    }
}
