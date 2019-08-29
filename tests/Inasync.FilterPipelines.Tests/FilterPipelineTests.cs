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
        public void Build_PredicateFuncs_Null() {
            new TestCaseRunner()
                .Run(() => FilterPipeline.Build((MiddlewareFunc<DummyContext, Task<PredicateFunc<DummyEntity>>>[])null))
                .Verify((actual, desc) => { }, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void Build_PredicateFuncs_Empty() {
            new TestCaseRunner()
                .Run(() => FilterPipeline.Build(new MiddlewareFunc<DummyContext, Task<PredicateFunc<DummyEntity>>>[0]))
                .Verify((actual, desc) => {
                    var actualPredicate = actual(new DummyContext()).GetAwaiter().GetResult();
                    Assert.AreEqual(PredicateComponent<DummyContext, DummyEntity>.NullPredicate, actualPredicate, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void Build_PredicateFuncs() {
            Action TestCase(int testNumber, (bool result, bool cancelled)[] fBehaviors, (bool result, int[] fIndexes) expected) => () => {
                var invokedPredicates = new List<SpyPredicateComponent>();
                var components = fBehaviors.Select(x => new SpyPredicateComponent(invokedPredicates, x.result, x.cancelled)).ToArray();
                var expectedPredicates = expected.fIndexes.Select(i => components[i]).ToArray();

                new TestCaseRunner($"No.{testNumber}")
                    .Run(() => FilterPipeline.Build(components.Select(c => c.Delegate)))
                    .Verify((actual, desc) => {
                        var context = new DummyContext();
                        var actualTask = actual(context);
                        Assert.IsTrue(invokedPredicates.All(x => x.ActualContext == context), desc);

                        var entity = new DummyEntity();
                        var actualResult = actualTask.GetAwaiter().GetResult()(entity);
                        Assert.AreEqual(expected.result, actualResult, desc);
                        CollectionAssert.AreEqual(expectedPredicates, invokedPredicates, desc);
                    }, (Type)null);
            };

            new[] {
                TestCase( 0, fBehaviors:new[]{ (result:true , cancelled:false), (result:true , cancelled:false) }, expected:(result:true , fIndexes:new[]{ 0, 1 })),  // 全て OK.
                TestCase( 1, fBehaviors:new[]{ (result:true , cancelled:false), (result:false, cancelled:false) }, expected:(result:false, fIndexes:new[]{ 0, 1 })),  // 2つ目 NG.
                TestCase( 2, fBehaviors:new[]{ (result:true , cancelled:true ), (result:false, cancelled:false) }, expected:(result:true , fIndexes:new[]{ 0    })),  // 1つ目 ショートサーキット.
                TestCase( 3, fBehaviors:new[]{ (result:false, cancelled:false), (result:true , cancelled:false) }, expected:(result:false, fIndexes:new[]{ 0    })),  // 1つ目 NG.
            }.Run();
        }

        [TestMethod]
        public void Build_FilterFuncs_Null() {
            new TestCaseRunner()
                .Run(() => FilterPipeline.Build((MiddlewareFunc<DummyContext, Task<FilterFunc<DummyEntity>>>[])null))
                .Verify((actual, desc) => { }, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void Build_FilterFuncs_Empty() {
            new TestCaseRunner()
                .Run(() => FilterPipeline.Build(new MiddlewareFunc<DummyContext, Task<FilterFunc<DummyEntity>>>[0]))
                .Verify((actual, desc) => {
                    var actualFilter = actual(new DummyContext()).GetAwaiter().GetResult();
                    Assert.AreEqual(FilterComponent<DummyContext, DummyEntity>.NullFilter, actualFilter, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void Build_FilterFuncs() {
            Action TestCase(int testNumber, (DummyEntity[] result, bool cancelled)[] fBehaviors, (DummyEntity[] result, int[] fIndexes) expected) => () => {
                var invokedFilters = new List<SpyFilterComponent>();
                var components = fBehaviors.Select(x => new SpyFilterComponent(invokedFilters, x.result, x.cancelled)).ToArray();
                var expectedFilters = expected.fIndexes.Select(i => components[i]).ToArray();

                new TestCaseRunner($"No.{testNumber}")
                    .Run(() => FilterPipeline.Build(components.Select(c => c.Delegate)))
                    .Verify((actual, desc) => {
                        var context = new DummyContext();
                        var actualTask = actual(context);
                        Assert.IsTrue(invokedFilters.All(x => x.ActualContext == context), desc);

                        var actualResult = actualTask.GetAwaiter().GetResult()(new[] { new DummyEntity(), new DummyEntity() });
                        Assert.AreEqual(expected.result, actualResult, desc);
                        CollectionAssert.AreEqual(expectedFilters, invokedFilters, desc);
                    }, (Type)null);
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

        private class SpyPredicateComponent : PredicateComponent<DummyContext, DummyEntity> {
            private readonly List<SpyPredicateComponent> _invokedPredicates;
            private readonly bool _result;
            private readonly bool _cancelled;

            public SpyPredicateComponent(List<SpyPredicateComponent> invokedPredicates, bool result, bool cancelled) {
                _invokedPredicates = invokedPredicates;
                _result = result;
                _cancelled = cancelled;
            }

            public MiddlewareFunc<DummyContext, Task<PredicateFunc<DummyEntity>>> Delegate => Middleware;

            public DummyContext ActualContext { get; private set; }

            protected override PredicateFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) {
                ActualContext = context;
                cancelled = _cancelled;

                return entity => {
                    _invokedPredicates.Add(this);
                    return _result;
                };
            }
        }

        private class SpyFilterComponent : FilterComponent<DummyContext, DummyEntity> {
            private readonly List<SpyFilterComponent> _invokedFilters;
            private readonly DummyEntity[] _result;
            private readonly bool _cancelled;

            public SpyFilterComponent(List<SpyFilterComponent> invokedFilters, DummyEntity[] result, bool cancelled) {
                _invokedFilters = invokedFilters;
                _result = result;
                _cancelled = cancelled;
            }

            public MiddlewareFunc<DummyContext, Task<FilterFunc<DummyEntity>>> Delegate => Middleware;

            public DummyContext ActualContext { get; private set; }

            protected override FilterFunc<DummyEntity> Create(DummyContext context, ref bool cancelled) {
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
