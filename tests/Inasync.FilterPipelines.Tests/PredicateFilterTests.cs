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
                .Run(() => PredicateFilter<DummyEntity, DummyContext>.NullFilter(entity))
                .Verify(true, (Type)null);
        }

        [TestMethod]
        public void CreateAsync() {
            var entity = new DummyEntity();

            Action TestCase(int testNumber, bool cancelled, bool result, bool nextResult, (bool result, DummyEntity nextEntity) expected) => () => {
                var filter = new SpyPredicateFilter(result, cancelled);
                var context = new DummyContext();

                DummyEntity actualNextEntity = default;
                Func<Task<Func<DummyEntity, bool>>> next = () => Task.FromResult<Func<DummyEntity, bool>>(x => {
                    actualNextEntity = x;
                    return nextResult;
                });

                new TestCaseRunner($"No.{testNumber}")
                    .Run(async () => (await filter.CreateAsync(context, next))(entity))
                    .Verify((actual, desc) => {
                        Assert.AreEqual(expected.result, actual, desc);

                        Assert.AreEqual(context, filter.ActualCreateContext, desc);
                        Assert.AreEqual(entity, filter.ActualEntity, desc);
                        Assert.AreEqual(expected.nextEntity, actualNextEntity, desc);
                    }, (Type)null);
            };

            new[] {
                TestCase( 0, cancelled: true , result: true , nextResult:default, expected:(true , null)),
                TestCase( 1, cancelled: true , result: false, nextResult:default, expected:(false, null)),
                TestCase( 2, cancelled: false, result: true , nextResult:true   , expected:(true , entity)),
                TestCase( 3, cancelled: false, result: true , nextResult:false  , expected:(false, entity)),
                TestCase( 4, cancelled: false, result: false, nextResult:true   , expected:(false, null)),
                TestCase( 5, cancelled: false, result: false, nextResult:false  , expected:(false, null)),
            }.Run();
        }

        [TestMethod]
        public void Create() {
            Func<DummyEntity, bool> nextFunc = _ => true;

            Action TestCase(int testNumber, bool cancelled, Func<DummyEntity, bool> expected) => () => {
                var filter = new DummyPredicateFilter(cancelled);
                var context = new DummyContext();
                Func<Task<Func<DummyEntity, bool>>> next = () => Task.FromResult(nextFunc);

                new TestCaseRunner($"No.{testNumber}")
                    .Run(() => filter.CreateAsync(context, next))
                    .Verify(expected, (Type)null);
            };

            new[] {
                TestCase( 0, cancelled:false, expected:nextFunc                     ),  // 既定ではフィルターしないので nextFunc が呼ばれる。
                TestCase( 1, cancelled:true , expected:SpyPredicateFilter.NullFilter),  // キャンセル時は nextFunc は呼ばれないので、既定の NullFilter が返却される。
            }.Run();
        }

        #region Helpers

        private sealed class SpyPredicateFilter : PredicateFilter<DummyEntity, DummyContext> {
            private readonly bool _result;
            private readonly bool _cancelled;

            public SpyPredicateFilter(bool result, bool cancelled) {
                _result = result;
                _cancelled = cancelled;
            }

            public DummyContext ActualCreateContext { get; private set; }
            public DummyEntity ActualEntity { get; private set; }

            protected override Func<DummyEntity, bool> Create(DummyContext context, ref bool cancelled) {
                ActualCreateContext = context;
                cancelled = _cancelled;

                return entity => {
                    ActualEntity = entity;
                    return _result;
                };
            }
        }

        private sealed class DummyPredicateFilter : PredicateFilter<DummyEntity, DummyContext> {
            private readonly bool _cancelled;

            public DummyPredicateFilter(bool cancelled) => _cancelled = cancelled;

            protected override Func<DummyEntity, bool> Create(DummyContext context, ref bool cancelled) {
                cancelled = _cancelled;
                return base.Create(context, ref cancelled);
            }
        }

        #endregion Helpers
    }
}
