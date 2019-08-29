using System;
using System.Threading.Tasks;
using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class PredicateFilterTests {

        [TestMethod]
        public void NullFilter() {
            var entity = new DummyEntity();

            new TestCaseRunner()
                .Run(() => PredicateFilter<DummyContext, DummyEntity>.NullFilter(entity))
                .Verify(true, (Type)null);
        }

        [TestMethod]
        public void Middleware() {
            PredicateFilterFunc<DummyEntity> createAsyncResult = _ => Rand.Bool();
            var filter = new SpyPredicateFilterToMiddleware(createAsyncResult);
            Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> next = _ => Task.FromResult<PredicateFilterFunc<DummyEntity>>(__ => Rand.Bool());
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
            PredicateFilterFunc<DummyEntity> filterFunc = _ => Rand.Bool();
            var filter = new SpyPredicateFilterToCreateAsync(createResult: filterFunc, cancelled: true);
            var context = new DummyContext();
            Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> next = _ => throw new AssertFailedException("next は呼ばれてはいけません。");

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
            var filter = new SpyPredicateFilterToCreateAsync(createResult: SpyPredicateFilterToCreateAsync.NullFilter, cancelled: false);
            var context = new DummyContext();

            PredicateFilterFunc<DummyEntity> nextFunc = _ => Rand.Bool();
            DummyContext actualNextContext = default;
            Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> next = ctx => {
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
            PredicateFilterFunc<DummyEntity> filterFunc = _ => Rand.Bool();
            var filter = new SpyPredicateFilterToCreateAsync(createResult: filterFunc, cancelled: false);
            var context = new DummyContext();

            DummyContext actualNextContext = default;
            Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(SpyPredicateFilterToCreateAsync.NullFilter);
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
            var context = new DummyContext();
            var entity = new DummyEntity();

            Action TestCase(int testNumber, bool createResult, bool nextResult, (bool result, DummyEntity nextEntity) expected) => () => {
                DummyEntity actualEntity = default;
                var filter = new SpyPredicateFilterToCreateAsync(x => {
                    actualEntity = x;
                    return createResult;
                }, cancelled: false);

                DummyContext actualNextContext = default;
                DummyEntity actualNextEntity = default;
                Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> next = ctx => {
                    actualNextContext = ctx;
                    return Task.FromResult<PredicateFilterFunc<DummyEntity>>(x => {
                        actualNextEntity = x;
                        return nextResult;
                    });
                };

                new TestCaseRunner($"No.{testNumber}")
                    .Run(async () => (await filter.CreateAsync(context, next))(entity))
                    .Verify((actual, desc) => {
                        Assert.AreEqual(expected.result, actual, desc);

                        Assert.AreEqual(context, filter.ActualContext, desc);
                        Assert.AreEqual(false, filter.ActualCancelled, desc);
                        Assert.AreEqual(entity, actualEntity, desc);
                        Assert.AreEqual(context, actualNextContext, desc);
                        Assert.AreEqual(expected.nextEntity, actualNextEntity, desc);
                    }, (Type)null);
            };

            new[] {
                TestCase( 0, createResult: true , nextResult:true , expected:(true , entity)),
                TestCase( 1, createResult: true , nextResult:false, expected:(false, entity)),
                TestCase( 2, createResult: false, nextResult:true , expected:(false, null  )),
                TestCase( 3, createResult: false, nextResult:false, expected:(false, null  )),
            }.Run();
        }

        [TestMethod]
        public void Create() {
            var filter = new FakePredicateFilterToCreate();
            var context = new DummyContext();
            var cancelled = Rand.Bool();

            new TestCaseRunner()
                .Run(() => filter.Create(context, ref cancelled))
                .Verify(FakePredicateFilterToCreate.NullFilter, (Type)null);
        }

        #region Helpers

        private sealed class SpyPredicateFilterToMiddleware : PredicateFilter<DummyContext, DummyEntity> {
            private readonly PredicateFilterFunc<DummyEntity> _createAsyncResult;

            public SpyPredicateFilterToMiddleware(PredicateFilterFunc<DummyEntity> createAsyncResult) => _createAsyncResult = createAsyncResult;

            public DummyContext ActualContext { get; private set; }
            public Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> ActualNext { get; private set; }

            protected override Task<PredicateFilterFunc<DummyEntity>> CreateAsync(DummyContext context, Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> next) {
                ActualContext = context;
                ActualNext = next;

                return Task.FromResult(_createAsyncResult);
            }
        }

        private sealed class SpyPredicateFilterToCreateAsync : PredicateFilter<DummyContext, DummyEntity> {
            private readonly PredicateFilterFunc<DummyEntity> _createResult;
            private readonly bool _cancelled;

            public SpyPredicateFilterToCreateAsync(PredicateFilterFunc<DummyEntity> createResult, bool cancelled) {
                _createResult = createResult;
                _cancelled = cancelled;
            }

            public DummyContext ActualContext { get; private set; }
            public bool ActualCancelled { get; private set; }

            public new Task<PredicateFilterFunc<DummyEntity>> CreateAsync(DummyContext context, Func<DummyContext, Task<PredicateFilterFunc<DummyEntity>>> next) => base.CreateAsync(context, next);

            protected override PredicateFilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) {
                ActualContext = context;
                ActualCancelled = cancelled;
                cancelled = _cancelled;

                return _createResult;
            }
        }

        private sealed class FakePredicateFilterToCreate : PredicateFilter<DummyContext, DummyEntity> {

            public new PredicateFilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) => base.Create(context, ref cancelled);
        }

        #endregion Helpers
    }
}
