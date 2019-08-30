using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class FilterMiddlewareTests {

        [TestMethod]
        public void NullFilter() {
            var source = Rand.Array<DummyEntity>();

            new TestCaseRunner()
                .Run(() => FilterMiddleware<DummyContext, DummyEntity>.NullFilter(source))
                .Verify(source, (Type)null);
        }

        [TestMethod]
        public void Middleware() {
            FilterFunc<DummyEntity> createAsyncResult = _ => Rand.Array<DummyEntity>();
            var middleware = new SpyFilterMiddlewareToMiddleware(createAsyncResult);
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = _ => Task.FromResult<FilterFunc<DummyEntity>>(__ => Rand.Array<DummyEntity>());
            var context = new DummyContext();

            new TestCaseRunner()
                .Run(() => middleware.Invoke(next)(context))
                .Verify((actual, desc) => {
                    Assert.AreEqual(createAsyncResult, actual, desc);

                    Assert.AreEqual(context, middleware.ActualContext, desc);
                    Assert.AreEqual(next, middleware.ActualNext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_Cancelled() {
            FilterFunc<DummyEntity> createResult = _ => Rand.Array<DummyEntity>();
            var middleware = new SpyFilterMiddlewareToCreateAsync(createResult, cancelled: true);
            var context = new DummyContext();
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = _ => throw new AssertFailedException("next は呼ばれてはいけません。");

            new TestCaseRunner()
                .Run(() => middleware.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(createResult, actual, desc);

                    Assert.AreEqual(context, middleware.ActualContext, desc);
                    Assert.AreEqual(false, middleware.ActualCancelled, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_NullFilter() {
            var middleware = new SpyFilterMiddlewareToCreateAsync(createResult: SpyFilterMiddlewareToCreateAsync.NullFilter, cancelled: false);
            var context = new DummyContext();

            FilterFunc<DummyEntity> nextFilter = _ => Rand.Array<DummyEntity>();
            DummyContext actualNextContext = default;
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(nextFilter);
            };

            new TestCaseRunner()
                .Run(() => middleware.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(nextFilter, actual, desc);

                    Assert.AreEqual(context, middleware.ActualContext, desc);
                    Assert.AreEqual(false, middleware.ActualCancelled, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_NullNextFunc() {
            FilterFunc<DummyEntity> createResult = _ => Rand.Array<DummyEntity>();
            var middleware = new SpyFilterMiddlewareToCreateAsync(createResult, cancelled: false);
            var context = new DummyContext();

            DummyContext actualNextContext = default;
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(SpyFilterMiddlewareToCreateAsync.NullFilter);
            };

            new TestCaseRunner()
                .Run(() => middleware.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(createResult, actual, desc);

                    Assert.AreEqual(context, middleware.ActualContext, desc);
                    Assert.AreEqual(false, middleware.ActualCancelled, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_Abyss() {
            var createResult = Rand.Array<DummyEntity>();
            IEnumerable<DummyEntity> actualSource = default;
            var middleware = new SpyFilterMiddlewareToCreateAsync(src => {
                actualSource = src;
                return createResult;
            }, cancelled: false);

            var nextResult = Rand.Array<DummyEntity>();
            DummyContext actualNextContext = default;
            IEnumerable<DummyEntity> actualNextSource = default;
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult<FilterFunc<DummyEntity>>(src => {
                    actualNextSource = src;
                    return nextResult;
                });
            };

            var context = new DummyContext();
            var source = new[] { new DummyEntity(), new DummyEntity() };

            new TestCaseRunner()
                .Run(async () => (await middleware.CreateAsync(context, next))(source))
                .Verify((actual, desc) => {
                    Assert.AreEqual(nextResult, actual, desc);

                    Assert.AreEqual(context, middleware.ActualContext, desc);
                    Assert.AreEqual(false, middleware.ActualCancelled, desc);
                    Assert.AreEqual(source, actualSource, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                    Assert.AreEqual(createResult, actualNextSource, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void Create() {
            var middleware = new FakeFilterMiddlewareToCreate();
            var context = new DummyContext();
            var cancelled = Rand.Bool();

            new TestCaseRunner()
                .Run(() => middleware.Create(context, ref cancelled))
                .Verify(FakeFilterMiddlewareToCreate.NullFilter, (Type)null);
        }

        #region Helpers

        private sealed class SpyFilterMiddlewareToMiddleware : FilterMiddleware<DummyContext, DummyEntity> {
            private readonly FilterFunc<DummyEntity> _createAsyncResult;

            public SpyFilterMiddlewareToMiddleware(FilterFunc<DummyEntity> createAsyncResult) => _createAsyncResult = createAsyncResult;

            public DummyContext ActualContext { get; private set; }
            public Func<DummyContext, Task<FilterFunc<DummyEntity>>> ActualNext { get; private set; }

            protected override Task<FilterFunc<DummyEntity>> CreateAsync(DummyContext context, Func<DummyContext, Task<FilterFunc<DummyEntity>>> next) {
                ActualContext = context;
                ActualNext = next;

                return Task.FromResult(_createAsyncResult);
            }
        }

        private class SpyFilterMiddlewareToCreateAsync : FilterMiddleware<DummyContext, DummyEntity> {
            private readonly FilterFunc<DummyEntity> _createResult;
            private readonly bool _cancelled;

            public SpyFilterMiddlewareToCreateAsync(FilterFunc<DummyEntity> createResult, bool cancelled) {
                _createResult = createResult;
                _cancelled = cancelled;
            }

            public DummyContext ActualContext { get; private set; }
            public bool ActualCancelled { get; private set; }

            public new Task<FilterFunc<DummyEntity>> CreateAsync(DummyContext context, Func<DummyContext, Task<FilterFunc<DummyEntity>>> next) => base.CreateAsync(context, next);

            protected override FilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) {
                ActualContext = context;
                ActualCancelled = cancelled;
                cancelled = _cancelled;

                return _createResult;
            }
        }

        private sealed class FakeFilterMiddlewareToCreate : FilterMiddleware<DummyContext, DummyEntity> {

            public new FilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) => base.Create(context, ref cancelled);
        }

        #endregion Helpers
    }
}
