using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class FilterComponentTests {

        [TestMethod]
        public void NullFilter() {
            var source = Rand.Array<DummyEntity>();

            new TestCaseRunner()
                .Run(() => FilterComponent<DummyContext, DummyEntity>.NullFilter(source))
                .Verify(source, (Type)null);
        }

        [TestMethod]
        public void Middleware() {
            FilterFunc<DummyEntity> createAsyncResult = _ => Rand.Array<DummyEntity>();
            var component = new SpyFilterComponentToMiddleware(createAsyncResult);
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = _ => Task.FromResult<FilterFunc<DummyEntity>>(__ => Rand.Array<DummyEntity>());
            var context = new DummyContext();

            new TestCaseRunner()
                .Run(() => component.Middleware(next)(context))
                .Verify((actual, desc) => {
                    Assert.AreEqual(createAsyncResult, actual, desc);

                    Assert.AreEqual(context, component.ActualContext, desc);
                    Assert.AreEqual(next, component.ActualNext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_Cancelled() {
            FilterFunc<DummyEntity> createResult = _ => Rand.Array<DummyEntity>();
            var component = new SpyFilterComponentToCreateAsync(createResult, cancelled: true);
            var context = new DummyContext();
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = _ => throw new AssertFailedException("next は呼ばれてはいけません。");

            new TestCaseRunner()
                .Run(() => component.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(createResult, actual, desc);

                    Assert.AreEqual(context, component.ActualContext, desc);
                    Assert.AreEqual(false, component.ActualCancelled, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_NullFilter() {
            var component = new SpyFilterComponentToCreateAsync(createResult: SpyFilterComponentToCreateAsync.NullFilter, cancelled: false);
            var context = new DummyContext();

            FilterFunc<DummyEntity> nextFilter = _ => Rand.Array<DummyEntity>();
            DummyContext actualNextContext = default;
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(nextFilter);
            };

            new TestCaseRunner()
                .Run(() => component.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(nextFilter, actual, desc);

                    Assert.AreEqual(context, component.ActualContext, desc);
                    Assert.AreEqual(false, component.ActualCancelled, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_NullNextFunc() {
            FilterFunc<DummyEntity> createResult = _ => Rand.Array<DummyEntity>();
            var component = new SpyFilterComponentToCreateAsync(createResult, cancelled: false);
            var context = new DummyContext();

            DummyContext actualNextContext = default;
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(SpyFilterComponentToCreateAsync.NullFilter);
            };

            new TestCaseRunner()
                .Run(() => component.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(createResult, actual, desc);

                    Assert.AreEqual(context, component.ActualContext, desc);
                    Assert.AreEqual(false, component.ActualCancelled, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_Abyss() {
            var createResult = Rand.Array<DummyEntity>();
            IEnumerable<DummyEntity> actualSource = default;
            var component = new SpyFilterComponentToCreateAsync(src => {
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
                .Run(async () => (await component.CreateAsync(context, next))(source))
                .Verify((actual, desc) => {
                    Assert.AreEqual(nextResult, actual, desc);

                    Assert.AreEqual(context, component.ActualContext, desc);
                    Assert.AreEqual(false, component.ActualCancelled, desc);
                    Assert.AreEqual(source, actualSource, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                    Assert.AreEqual(createResult, actualNextSource, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void Create() {
            var component = new FakeFilterComponentToCreate();
            var context = new DummyContext();
            var cancelled = Rand.Bool();

            new TestCaseRunner()
                .Run(() => component.Create(context, ref cancelled))
                .Verify(FakeFilterComponentToCreate.NullFilter, (Type)null);
        }

        #region Helpers

        private sealed class SpyFilterComponentToMiddleware : FilterComponent<DummyContext, DummyEntity> {
            private readonly FilterFunc<DummyEntity> _createAsyncResult;

            public SpyFilterComponentToMiddleware(FilterFunc<DummyEntity> createAsyncResult) => _createAsyncResult = createAsyncResult;

            public DummyContext ActualContext { get; private set; }
            public Func<DummyContext, Task<FilterFunc<DummyEntity>>> ActualNext { get; private set; }

            protected override Task<FilterFunc<DummyEntity>> CreateAsync(DummyContext context, Func<DummyContext, Task<FilterFunc<DummyEntity>>> next) {
                ActualContext = context;
                ActualNext = next;

                return Task.FromResult(_createAsyncResult);
            }
        }

        private class SpyFilterComponentToCreateAsync : FilterComponent<DummyContext, DummyEntity> {
            private readonly FilterFunc<DummyEntity> _createResult;
            private readonly bool _cancelled;

            public SpyFilterComponentToCreateAsync(FilterFunc<DummyEntity> createResult, bool cancelled) {
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

        private sealed class FakeFilterComponentToCreate : FilterComponent<DummyContext, DummyEntity> {

            public new FilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) => base.Create(context, ref cancelled);
        }

        #endregion Helpers
    }
}
