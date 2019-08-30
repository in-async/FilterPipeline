using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class Usage {

        [TestMethod]
        public async Task Usage_Readme() {
            Func<Uri, Task<PredicateFunc<FileInfo>>> predicateCreator = FilterPipeline.Build(new MiddlewareFunc<Uri, Task<PredicateFunc<FileInfo>>>[]{
                next => async context => {
                    if (!context.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) { return _ => false; }
                    return await next(context);
                },
                next => async context => {
                    var nextPredicate = await next(context);
                    var uriPath = context.AbsolutePath.TrimStart('/');
                    return file => Path.GetFileNameWithoutExtension(file.Name).Equals(uriPath, StringComparison.OrdinalIgnoreCase) && nextPredicate(file);
                },
            });

            var predicate = await predicateCreator(new Uri("https://example.com/foo"));
            Assert.AreEqual(true, predicate(new FileInfo("foo.html")));
            Assert.AreEqual(false, predicate(new FileInfo("bar.html")));

            Assert.AreEqual(false, (await predicateCreator(new Uri("http://example.com/foo")))(new FileInfo("foo.html")));
        }
    }
}
