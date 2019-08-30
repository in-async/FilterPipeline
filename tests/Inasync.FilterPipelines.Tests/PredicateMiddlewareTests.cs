using System;
using System.Threading.Tasks;
using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class PredicateMiddlewareTests {

        [TestMethod]
        public void NullPredicate() {
            var entity = new DummyEntity();

            new TestCaseRunner()
                .Run(() => PredicateMiddleware<DummyContext, DummyEntity>.NullPredicate(entity))
                .Verify(true, (Type)null);
        }

        [TestMethod]
        public void Middleware() {
            PredicateFunc<DummyEntity> createAsyncResult = _ => Rand.Bool();
            var middleware = new SpyPredicateMiddlewareToMiddleware(createAsyncResult);
            Func<DummyContext, Task<PredicateFunc<DummyEntity>>> next = _ => Task.FromResult<PredicateFunc<DummyEntity>>(__ => Rand.Bool());
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
            PredicateFunc<DummyEntity> createResult = _ => Rand.Bool();
            var middleware = new SpyPredicateMiddlewareToCreateAsync(createResult, cancelled: true);
            var context = new DummyContext();
            Func<DummyContext, Task<PredicateFunc<DummyEntity>>> next = _ => throw new AssertFailedException("next は呼ばれてはいけません。");

            new TestCaseRunner()
                .Run(() => middleware.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(createResult, actual, desc);

                    Assert.AreEqual(context, middleware.ActualContext, desc);
                    Assert.AreEqual(false, middleware.ActualCancelled, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_NullPredicate() {
            var middleware = new SpyPredicateMiddlewareToCreateAsync(createResult: SpyPredicateMiddlewareToCreateAsync.NullPredicate, cancelled: false);
            var context = new DummyContext();

            PredicateFunc<DummyEntity> nextPredicate = _ => Rand.Bool();
            DummyContext actualNextContext = default;
            Func<DummyContext, Task<PredicateFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(nextPredicate);
            };

            new TestCaseRunner()
                .Run(() => middleware.CreateAsync(context, next))
                .Verify((actual, desc) => {
                    Assert.AreEqual(nextPredicate, actual, desc);

                    Assert.AreEqual(context, middleware.ActualContext, desc);
                    Assert.AreEqual(false, middleware.ActualCancelled, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void CreateAsync_NullNextPredicate() {
            PredicateFunc<DummyEntity> createResult = _ => Rand.Bool();
            var middleware = new SpyPredicateMiddlewareToCreateAsync(createResult, cancelled: false);
            var context = new DummyContext();

            DummyContext actualNextContext = default;
            Func<DummyContext, Task<PredicateFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(SpyPredicateMiddlewareToCreateAsync.NullPredicate);
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
            var context = new DummyContext();
            var entity = new DummyEntity();

            Action TestCase(int testNumber, bool createResult, bool nextResult, (bool result, DummyEntity nextEntity) expected) => () => {
                DummyEntity actualEntity = default;
                var middleware = new SpyPredicateMiddlewareToCreateAsync(x => {
                    actualEntity = x;
                    return createResult;
                }, cancelled: false);

                DummyContext actualNextContext = default;
                DummyEntity actualNextEntity = default;
                Func<DummyContext, Task<PredicateFunc<DummyEntity>>> next = ctx => {
                    actualNextContext = ctx;
                    return Task.FromResult<PredicateFunc<DummyEntity>>(x => {
                        actualNextEntity = x;
                        return nextResult;
                    });
                };

                new TestCaseRunner($"No.{testNumber}")
                    .Run(async () => (await middleware.CreateAsync(context, next))(entity))
                    .Verify((actual, desc) => {
                        Assert.AreEqual(expected.result, actual, desc);

                        Assert.AreEqual(context, middleware.ActualContext, desc);
                        Assert.AreEqual(false, middleware.ActualCancelled, desc);
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
            var middleware = new FakePredicateMiddlewareToCreate();
            var context = new DummyContext();
            var cancelled = Rand.Bool();

            new TestCaseRunner()
                .Run(() => middleware.Create(context, ref cancelled))
                .Verify(FakePredicateMiddlewareToCreate.NullPredicate, (Type)null);
        }

        #region Helpers

        private sealed class SpyPredicateMiddlewareToMiddleware : PredicateMiddleware<DummyContext, DummyEntity> {
            private readonly PredicateFunc<DummyEntity> _createAsyncResult;

            public SpyPredicateMiddlewareToMiddleware(PredicateFunc<DummyEntity> createAsyncResult) => _createAsyncResult = createAsyncResult;

            public DummyContext ActualContext { get; private set; }
            public Func<DummyContext, Task<PredicateFunc<DummyEntity>>> ActualNext { get; private set; }

            protected override Task<PredicateFunc<DummyEntity>> CreateAsync(DummyContext context, Func<DummyContext, Task<PredicateFunc<DummyEntity>>> next) {
                ActualContext = context;
                ActualNext = next;

                return Task.FromResult(_createAsyncResult);
            }
        }

        private sealed class SpyPredicateMiddlewareToCreateAsync : PredicateMiddleware<DummyContext, DummyEntity> {
            private readonly PredicateFunc<DummyEntity> _createResult;
            private readonly bool _cancelled;

            public SpyPredicateMiddlewareToCreateAsync(PredicateFunc<DummyEntity> createResult, bool cancelled) {
                _createResult = createResult;
                _cancelled = cancelled;
            }

            public DummyContext ActualContext { get; private set; }
            public bool ActualCancelled { get; private set; }

            public new Task<PredicateFunc<DummyEntity>> CreateAsync(DummyContext context, Func<DummyContext, Task<PredicateFunc<DummyEntity>>> next) => base.CreateAsync(context, next);

            protected override PredicateFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) {
                ActualContext = context;
                ActualCancelled = cancelled;
                cancelled = _cancelled;

                return _createResult;
            }
        }

        private sealed class FakePredicateMiddlewareToCreate : PredicateMiddleware<DummyContext, DummyEntity> {

            public new PredicateFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) => base.Create(context, ref cancelled);
        }

        #endregion Helpers
    }
}
