using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class Usage {

        [TestMethod]
        public async Task Usage_Readme() {
            Func<object, Task<PredicateFilterFunc<int>>> pipeline = FilterPipeline.Build(new MiddlewareFunc<object, Task<PredicateFilterFunc<int>>>[]{
                next => async context => {
                    var nextFunc = await next(context);
                    return num => (num % 4 == 0) && nextFunc(num);
                },
                next => async context => {
                    var nextFunc = await next(context);
                    return num => (num % 3 == 0) && nextFunc(num);
                },
            });
            var pipelineFunc = await pipeline(new object());

            Assert.AreEqual(true, pipelineFunc(24));
            Assert.AreEqual(false, pipelineFunc(30));
        }
    }
}
