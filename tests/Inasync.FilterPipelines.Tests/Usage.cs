using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class Usage {

        [TestMethod]
        public async Task Usage_Readme() {
            Func<object, Task<PredicateFunc<int>>> pipeline = FilterPipeline.Build(new MiddlewareFunc<object, Task<PredicateFunc<int>>>[]{
                next => async context => {
                    var nextPredicate = await next(context);
                    return num => (num % 4 == 0) && nextPredicate(num);
                },
                next => async context => {
                    var nextPredicate = await next(context);
                    return num => (num % 3 == 0) && nextPredicate(num);
                },
            });
            var predicate = await pipeline(new object());

            Assert.AreEqual(true, predicate(24));
            Assert.AreEqual(false, predicate(30));
        }
    }
}
