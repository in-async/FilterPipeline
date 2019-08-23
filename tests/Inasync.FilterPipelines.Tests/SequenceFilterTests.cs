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
                .Run(() => SequenceFilter<DummyEntity, DummyContext>.NullFilter(source))
                .Verify(source, (Type)null);
        }

        [TestMethod]
        public void Middleware() {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void CreateAsync() {
            var source = new[] { new DummyEntity(), new DummyEntity() };

            Action TestCase(int testNumber, bool cancelled, DummyEntity[] result, DummyEntity[] expectedNextSource) => () => {
                var filter = new SpySequenceFilter(result, cancelled);
                var context = new DummyContext();

                IEnumerable<DummyEntity> actualNextSource = default;
                Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next = ctx => Task.FromResult<SequenceFilterFunc<DummyEntity>>(src => {
                    actualNextSource = src;
                    return src;
                });

                new TestCaseRunner($"No.{testNumber}")
                    .Run(async () => (await filter.Middleware(next)(context))(source))
                    .Verify((actual, desc) => {
                        Assert.AreEqual(result, actual, desc);

                        Assert.AreEqual(context, filter.ActualContext, desc);
                        Assert.AreEqual(source, filter.ActualSource, desc);
                        Assert.AreEqual(expectedNextSource, actualNextSource, desc);
                    }, (Type)null);
            };

            var source2 = new[] { new DummyEntity() };
            new[]{
                TestCase( 0, cancelled: true , result: source , expectedNextSource: null   ),
                TestCase( 1, cancelled: false, result: source , expectedNextSource: source ),
                TestCase( 2, cancelled: false, result: source2, expectedNextSource: source2),
            }.Run();
        }

        [TestMethod]
        public void Create() {
            SequenceFilterFunc<DummyEntity> nextFunc = src => src;

            Action TestCase(int testNumber, bool cancelled, SequenceFilterFunc<DummyEntity> expected) => () => {
                var filter = new DummySequenceFilter(cancelled);
                var context = new DummyContext();
                Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>> next = _ => Task.FromResult(nextFunc);

                new TestCaseRunner($"No.{testNumber}")
                    .Run(() => filter.Middleware(next)(context))
                    .Verify(expected, (Type)null);
            };

            new[] {
                TestCase( 0, cancelled:false, expected:nextFunc                    ),  // 既定ではフィルターしないので nextFunc が呼ばれる。
                TestCase( 1, cancelled:true , expected:SpySequenceFilter.NullFilter),  // キャンセル時は nextFunc は呼ばれないので、既定の NullFilter が返却される。
            }.Run();
        }

        #region Helpers

        private class SpySequenceFilter : SequenceFilter<DummyEntity, DummyContext> {
            private readonly DummyEntity[] _result;
            private readonly bool _cancelled;

            public SpySequenceFilter(DummyEntity[] result, bool cancelled) {
                _result = result;
                _cancelled = cancelled;
            }

            public DummyContext ActualContext { get; private set; }
            public IEnumerable<DummyEntity> ActualSource { get; private set; }

            protected override SequenceFilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) {
                ActualContext = context;
                cancelled = _cancelled;

                return source => {
                    ActualSource = source;
                    return _result;
                };
            }
        }

        private sealed class DummySequenceFilter : SequenceFilter<DummyEntity, DummyContext> {
            private readonly bool _cancelled;

            public DummySequenceFilter(bool cancelled) => _cancelled = cancelled;

            protected override SequenceFilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) {
                cancelled = _cancelled;
                return base.Create(context, ref cancelled);
            }
        }

        #endregion Helpers
    }
}
