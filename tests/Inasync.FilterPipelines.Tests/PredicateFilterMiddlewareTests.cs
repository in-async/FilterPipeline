using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Inasync;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class PredicateFilterMiddlewareTests {

        [TestMethod]
        public void Ctor() {
            Action TestCase(int testNumber, SpyPredicateCreator predicateCreator, Type expectedExceptionType = null) => () => {
                new TestCaseRunner($"No.{testNumber}")
                    .Run(() => new PredicateFilterMiddleware<DummyContext, DummyEntity>(predicateCreator?.Invoke))
                    .Verify((_, __) => { }, expectedExceptionType);
            };

            new[] {
                TestCase( 0, null                              , typeof(ArgumentNullException)),
                TestCase( 1, new SpyPredicateCreator(_ => true)),
            }.Run();
        }

        [TestMethod]
        public void Invoke_NullPredicate() {
            var predicateCreator = new SpyPredicateCreator(PredicateMiddleware<DummyContext, DummyEntity>.NullPredicate);
            var middleware = new PredicateFilterMiddleware<DummyContext, DummyEntity>(predicateCreator.Invoke);
            var context = new DummyContext();

            FilterFunc<DummyEntity> nextFilter = _ => Rand.Array<DummyEntity>();
            DummyContext actualNextContext = default;
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = ctx => {
                actualNextContext = ctx;
                return Task.FromResult(nextFilter);
            };

            new TestCaseRunner()
                .Run(() => middleware.Invoke(next)(context))
                .Verify((actual, desc) => {
                    Assert.AreEqual(nextFilter, actual, desc);

                    Assert.AreEqual(context, predicateCreator.ActualContext, desc);
                    Assert.AreEqual(context, actualNextContext, desc);
                }, (Type)null);
        }

        [TestMethod]
        public void Invoke_Abyss() {
            var actualPredicateEntities = new List<DummyEntity>();
            var predicateCreator = new SpyPredicateCreator(x => {
                actualPredicateEntities.Add(x);
                return true;
            });
            var middleware = new PredicateFilterMiddleware<DummyContext, DummyEntity>(predicateCreator.Invoke);

            var nextFilterResult = Rand.Array<DummyEntity>();
            DummyContext actualNextFilterContext = default;
            DummyEntity[] actualNextFilterSource = default;
            Func<DummyContext, Task<FilterFunc<DummyEntity>>> next = ctx => {
                actualNextFilterContext = ctx;
                return Task.FromResult<FilterFunc<DummyEntity>>(src => {
                    actualNextFilterSource = src.ToArray();
                    return nextFilterResult;
                });
            };

            var context = new DummyContext();
            var source = Rand.Array<DummyEntity>(minLength: 2);

            new TestCaseRunner()
                .Run(async () => (await middleware.Invoke(next)(context))(source))
                .Verify((actual, desc) => {
                    Assert.AreEqual(nextFilterResult, actual, desc);

                    Assert.AreEqual(context, predicateCreator.ActualContext, desc);
                    CollectionAssert.AreEqual(source, actualPredicateEntities, desc);

                    Assert.AreEqual(context, actualNextFilterContext, desc);
                    CollectionAssert.AreEqual(source, actualNextFilterSource);
                }, (Type)null);
        }

        #region Helpers

        private sealed class SpyPredicateCreator {
            private readonly PredicateFunc<DummyEntity> _predicate;

            public SpyPredicateCreator(PredicateFunc<DummyEntity> predicate) => _predicate = predicate;

            public DummyContext ActualContext { get; private set; }

            public Func<DummyContext, Task<PredicateFunc<DummyEntity>>> Invoke => context => {
                ActualContext = context;

                return Task.FromResult(_predicate);
            };
        }

        #endregion Helpers
    }
}
