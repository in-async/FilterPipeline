using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class FilterPipelineTests {

        [TestMethod]
        public void Build_PredicateFilters_Null() {
            TestRun("by interfaces", () => FilterPipeline.Build((SpyPredicateFilter[])null));
            TestRun("by delegates", () => FilterPipeline.Build((PredicateFilterCreator<DummyEntity, DummyContext>[])null));

            void TestRun(string desc, Func<Func<DummyContext, Task<Func<DummyEntity, bool>>>> targetCode) => new TestCaseRunner()
                .Run(targetCode)
                .Verify((actual, _) => { }, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void Build_PredicateFilters_Empty() {
            TestRun("by interfaces", () => FilterPipeline.Build(new SpyPredicateFilter[0]));
            TestRun("by delegates", () => FilterPipeline.Build(new PredicateFilterCreator<DummyEntity, DummyContext>[0]));

            void TestRun(string desc, Func<Func<DummyContext, Task<Func<DummyEntity, bool>>>> targetCode) => new TestCaseRunner()
                .Run(targetCode)
                .Verify((actual, _) => {
                    var actualFilterFunc = actual(new DummyContext()).GetAwaiter().GetResult();
                    Assert.AreEqual(PredicateFilter<DummyEntity, DummyContext>.NullFilter, actualFilterFunc);
                }, (Type)null);
        }

        [TestMethod]
        public void Build_PredicateFilters() {
            Action TestCase(int testNumber, (bool result, bool cancelled)[] fBehaviors, (bool result, int[] fIndexes) expected) => () => {
                TestRun("by interfaces", filters => FilterPipeline.Build(filters));
                TestRun("by delegates", filters => FilterPipeline.Build(filters.Select(f => f.Delegate)));

                void TestRun(string desc, Func<SpyPredicateFilter[], Func<DummyContext, Task<Func<DummyEntity, bool>>>> targetCode) {
                    var invokedFilters = new List<SpyPredicateFilter>();
                    var filters = fBehaviors.Select(x => new SpyPredicateFilter(invokedFilters, x.result, x.cancelled)).ToArray();
                    var expectedFilters = expected.fIndexes.Select(i => filters[i]).ToArray();

                    new TestCaseRunner($"No.{testNumber} {desc}")
                        .Run(() => targetCode(filters))
                        .Verify((actual, subDesc) => {
                            var context = new DummyContext();
                            var actualTask = actual(context);
                            Assert.IsTrue(invokedFilters.All(x => x.ActualContext == context), subDesc);

                            var entity = new DummyEntity();
                            var actualResult = actualTask.GetAwaiter().GetResult()(entity);
                            Assert.AreEqual(expected.result, actualResult, subDesc);
                            CollectionAssert.AreEqual(expectedFilters, invokedFilters, subDesc);
                        }, (Type)null);
                }
            };

            new[] {
                TestCase( 0, fBehaviors:new[]{ (result:true , cancelled:false), (result:true , cancelled:false) }, expected:(result:true , fIndexes:new[]{ 0, 1 })),  // 全て OK.
                TestCase( 1, fBehaviors:new[]{ (result:true , cancelled:false), (result:false, cancelled:false) }, expected:(result:false, fIndexes:new[]{ 0, 1 })),  // 2つ目 NG.
                TestCase( 2, fBehaviors:new[]{ (result:true , cancelled:true ), (result:false, cancelled:false) }, expected:(result:true , fIndexes:new[]{ 0    })),  // 1つ目 ショートサーキット.
                TestCase( 3, fBehaviors:new[]{ (result:false, cancelled:false), (result:true , cancelled:false) }, expected:(result:false, fIndexes:new[]{ 0    })),  // 1つ目 NG.
            }.Run();
        }

        [TestMethod]
        public void Build_SequenceFilters_Null() {
            TestRun("by interfaces", () => FilterPipeline.Build((SpySequenceFilter[])null));
            TestRun("by delegates", () => FilterPipeline.Build((SequenceFilterCreator<DummyEntity, DummyContext>[])null));

            void TestRun(string desc, Func<Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>>> targetCode) => new TestCaseRunner()
                .Run(targetCode)
                .Verify((actual, _) => { }, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void Build_SequenceFilters_Empty() {
            TestRun("by interfaces", () => FilterPipeline.Build(new SpySequenceFilter[0]));
            TestRun("by delegates", () => FilterPipeline.Build(new SequenceFilterCreator<DummyEntity, DummyContext>[0]));

            void TestRun(string desc, Func<Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>>> targetCode) => new TestCaseRunner()
                .Run(targetCode)
                .Verify((actual, _) => {
                    var actualFilterFunc = actual(new DummyContext()).GetAwaiter().GetResult();
                    Assert.AreEqual(SequenceFilter<DummyEntity, DummyContext>.NullFilter, actualFilterFunc);
                }, (Type)null);
        }

        [TestMethod]
        public void Build_SequenceFilters() {
            Action TestCase(int testNumber, (DummyEntity[] result, bool cancelled)[] fBehaviors, (DummyEntity[] result, int[] fIndexes) expected) => () => {
                TestRun("by interfaces", filters => FilterPipeline.Build(filters));
                TestRun("by delegates", filters => FilterPipeline.Build(filters.Select(f => f.Delegate)));

                void TestRun(string desc, Func<SpySequenceFilter[], Func<DummyContext, Task<SequenceFilterFunc<DummyEntity>>>> targetCode) {
                    var invokedFilters = new List<SpySequenceFilter>();
                    var filters = fBehaviors.Select(x => new SpySequenceFilter(invokedFilters, x.result, x.cancelled)).ToArray();
                    var expectedFilters = expected.fIndexes.Select(i => filters[i]).ToArray();

                    new TestCaseRunner($"No.{testNumber} {desc}")
                        .Run(() => targetCode(filters))
                        .Verify((actual, subDesc) => {
                            var context = new DummyContext();
                            var actualTask = actual(context);
                            Assert.IsTrue(invokedFilters.All(x => x.ActualContext == context), subDesc);

                            var actualResult = actualTask.GetAwaiter().GetResult()(new[] { new DummyEntity(), new DummyEntity() });
                            Assert.AreEqual(expected.result, actualResult, subDesc);
                            CollectionAssert.AreEqual(expectedFilters, invokedFilters, subDesc);
                        }, (Type)null);
                }
            };

            var source = new[] { new DummyEntity() };
            var source2 = new[] { new DummyEntity(), new DummyEntity() };
            new[] {
                TestCase( 1, fBehaviors:new[]{ (result:source , cancelled:false), (result:source2, cancelled:false) }, expected:(result:source2, fIndexes:new[]{ 0, 1 })),  // 最後のフィルター結果を反映。
                TestCase( 2, fBehaviors:new[]{ (result:source2, cancelled:false), (result:source , cancelled:false) }, expected:(result:source , fIndexes:new[]{ 0, 1 })),  // 最後のフィルター結果を反映。
                TestCase( 3, fBehaviors:new[]{ (result:source , cancelled:true ), (result:source2, cancelled:false) }, expected:(result:source , fIndexes:new[]{ 0    })),  // 1つ目 ショートサーキット.
            }.Run();
        }

        #region Helpers

        private class SpyPredicateFilter : PredicateFilter<DummyEntity, DummyContext> {
            private readonly List<SpyPredicateFilter> _invokedFilters;
            private readonly bool _result;
            private readonly bool _cancelled;

            public SpyPredicateFilter(List<SpyPredicateFilter> invokedFilters, bool result, bool cancelled) {
                _invokedFilters = invokedFilters;
                _result = result;
                _cancelled = cancelled;
            }

            public PredicateFilterCreator<DummyEntity, DummyContext> Delegate => CreateAsync;

            public DummyContext ActualContext { get; private set; }

            protected override Func<DummyEntity, bool> Create(DummyContext context, ref bool cancelled) {
                ActualContext = context;
                cancelled = _cancelled;

                return entity => {
                    _invokedFilters.Add(this);
                    return _result;
                };
            }
        }

        private class SpySequenceFilter : SequenceFilter<DummyEntity, DummyContext> {
            private readonly List<SpySequenceFilter> _invokedFilters;
            private readonly DummyEntity[] _result;
            private readonly bool _cancelled;

            public SpySequenceFilter(List<SpySequenceFilter> invokedFilters, DummyEntity[] result, bool cancelled) {
                _invokedFilters = invokedFilters;
                _result = result;
                _cancelled = cancelled;
            }

            public SequenceFilterCreator<DummyEntity, DummyContext> Delegate => CreateAsync;

            public DummyContext ActualContext { get; private set; }

            protected override SequenceFilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) {
                ActualContext = context;
                cancelled = _cancelled;

                return source => {
                    _invokedFilters.Add(this);
                    return _result;
                };
            }
        }

        #endregion Helpers
    }
}
