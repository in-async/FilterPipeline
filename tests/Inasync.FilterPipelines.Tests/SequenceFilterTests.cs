using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class SequenceFilterTests {

        [TestMethod]
        public void NullFilter() {
            var source = Rand.Array(_ => new DummyEntity());

            new TestCaseRunner()
                .Run(() => SequenceFilter<DummyContext, DummyEntity>.NullFilter(source))
                .Verify(source, (Type)null);
        }

        [TestMethod]
        public void Middleware() {
            SequenceFilterFunc<DummyEntity> createAsyncResult = _ => Rand.Array<DummyEntity>();
            var filter = new SpySequenceFilterToMiddleware(createAsyncResult);
            Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next = _ => Task.FromResult<SequenceFilterFunc<DummyEntity>>(__ => Rand.Array(___ => new DummyEntity()));
            var context = new DummyContext();

            new TestCaseRunner()
                .Run(() => filter.Middleware(next)(context))
                .Verify((actual, desc) => {
                    Assert.AreEqual(createAsyncResult, actual, desc);

                    Assert.AreEqual(context, filter.ActualContext, desc);
                    Assert.AreEqual(next, filter.ActualNext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_Cancelled() {
            SequenceFilterFunc<DummyEntity> filterFunc = _ => Rand.Array<DummyEntity>();
            var filter = new SpySequenceFilterToCreateAsync(createResult: filterFunc, cancelled: true);
            var context = new DummyContext();
            Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next = _ => throw new AssertFailedException("next は呼ばれてはいけません。");

            new TestCaseRunner()
                .Run(() => filter.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(filterFunc, actual, desc);

                    Assert.AreEqual(context, filter.ActualContext, desc);
                    Assert.AreEqual(false, filter.ActualCancelled, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_NullFilterFunc() {
            var filter = new SpySequenceFilterToCreateAsync(createResult: SpySequenceFilterToCreateAsync.NullFilter, cancelled: false);
            var context = new DummyContext();

            SequenceFilterFunc<DummyEntity> nextFunc = _ => Rand.Array<DummyEntity>();
            DummyContext actualNextContext = default;
            Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(nextFunc);
            };

            new TestCaseRunner()
                .Run(() => filter.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(nextFunc, actual, desc);

                    Assert.AreEqual(context, filter.ActualContext, desc);
                    Assert.AreEqual(false, filter.ActualCancelled, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_NullNextFunc() {
            SequenceFilterFunc<DummyEntity> filterFunc = _ => Rand.Array<DummyEntity>();
            var filter = new SpySequenceFilterToCreateAsync(createResult: filterFunc, cancelled: false);
            var context = new DummyContext();

            DummyContext actualNextContext = default;
            Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(SpySequenceFilterToCreateAsync.NullFilter);
            };

            new TestCaseRunner()
                .Run(() => filter.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(filterFunc, actual, desc);

                    Assert.AreEqual(context, filter.ActualContext, desc);
                    Assert.AreEqual(false, filter.ActualCancelled, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_Abyss() {
            var createResult = Rand.Array<DummyEntity>();
            IEnumerable<DummyEntity> actualSource = default;
            var filter = new SpySequenceFilterToCreateAsync(src => {
                actualSource = src;
                return createResult;
            }, cancelled: false);

            var nextResult = Rand.Array<DummyEntity>();
            DummyContext actualNextContext = default;
            IEnumerable<DummyEntity> actualNextSource = default;
            Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult<SequenceFilterFunc<DummyEntity>>(src => {
                    actualNextSource = src;
                    return nextResult;
                });
            };

            var context = new DummyContext();
            var source = new[] { new DummyEntity(), new DummyEntity() };

            new TestCaseRunner()
                .Run(async () => (await filter.CreateAsync(context, next))(source))
                .Verify((actual, desc) => {
                    Assert.AreEqual(nextResult, actual, desc);

                    Assert.AreEqual(context, filter.ActualContext, desc);
                    Assert.AreEqual(false, filter.ActualCancelled, desc);
                    Assert.AreEqual(source, actualSource, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                    Assert.AreEqual(createResult, actualNextSource, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void Create() {
            var filter = new FakeSequenceFilterToCreate();
            var context = new DummyContext();
            var cancelled = Rand.Bool();

            new TestCaseRunner()
                .Run(() => filter.Create(context, ref cancelled))
                .Verify(FakeSequenceFilterToCreate.NullFilter, (Type)null);
        }

        #region Helpers

        private sealed class SpySequenceFilterToMiddleware : SequenceFilter<DummyContext, DummyEntity> {
            private readonly SequenceFilterFunc<DummyEntity> _createAsyncResult;

            public SpySequenceFilterToMiddleware(SequenceFilterFunc<DummyEntity> createAsyncResult) => _createAsyncResult = createAsyncResult;

            public DummyContext ActualContext { get; private set; }
            public Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> ActualNext { get; private set; }

            protected override Task<SequenceFilterFunc<DummyEntity>> CreateAsync(DummyContext context, Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next) {
                ActualContext = context;
                ActualNext = next;

                return Task.FromResult(_createAsyncResult);
            }
        }

        private class SpySequenceFilterToCreateAsync : SequenceFilter<DummyContext, DummyEntity> {
            private readonly SequenceFilterFunc<DummyEntity> _createResult;
            private readonly bool _cancelled;

            public SpySequenceFilterToCreateAsync(SequenceFilterFunc<DummyEntity> createResult, bool cancelled) {
                _createResult = createResult;
                _cancelled = cancelled;
            }

            public DummyContext ActualContext { get; private set; }
            public bool ActualCancelled { get; private set; }

            public new Task<SequenceFilterFunc<DummyEntity>> CreateAsync(DummyContext context, Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next) => base.CreateAsync(context, next);

            protected override SequenceFilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) {
                ActualContext = context;
                ActualCancelled = cancelled;
                cancelled = _cancelled;

                return _createResult;
            }
        }

        private sealed class FakeSequenceFilterToCreate : SequenceFilter<DummyContext, DummyEntity> {

            public new SequenceFilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) => base.Create(context, ref cancelled);
        }

        #endregion Helpers
    }
}
