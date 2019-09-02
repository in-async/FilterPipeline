using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.Tests {

    [TestClass]
    public class MiddlewareExtensionsTests {

        [TestMethod]
        public void ToDelegate_Null() {
            new TestCaseRunner()
                .Run(() => MiddlewareExtensions.ToDelegate((SpyMiddleware)null))
                .Verify((actual, desc) => { }, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void ToDelegate() {
            Func<Dummy, DummyResult> middlewareResult = _ => new DummyResult();
            var middleware = new SpyMiddleware(middlewareResult);
            Func<Dummy, DummyResult> next = _ => new DummyResult();

            new TestCaseRunner()
                .Run(() => MiddlewareExtensions.ToDelegate(middleware)(next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(middlewareResult, actual, desc);

                    Assert.AreEqual(next, middleware.ActualNext, desc);
                }, (Type)null);
        }

        #region Helpers

        private sealed class SpyMiddleware : IMiddleware<Dummy, DummyResult> {
            private readonly Func<Dummy, DummyResult> _result;

            public SpyMiddleware(Func<Dummy, DummyResult> result) => _result = result;

            public Func<Dummy, DummyResult> ActualNext { get; private set; }

            public Func<Dummy, DummyResult> Invoke(Func<Dummy, DummyResult> next) {
                ActualNext = next;
                return _result;
            }
        }

        private class Dummy { }

        private class DummyResult { }

        #endregion Helpers
    }
}
