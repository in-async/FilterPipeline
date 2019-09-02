using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.FilterPipelines.Tests {

    [TestClass]
    public class Usage {

        [TestMethod]
        public async Task Usage_MiddlewareFunc() {
            Func<Uri, Task<PredicateFunc<FileInfo>>> pipeline = FilterPipeline.Build(new MiddlewareFunc<Uri, Task<PredicateFunc<FileInfo>>>[]{
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

            var predicate = await pipeline(new Uri("https://example.com/foo"));
            Assert.AreEqual(true, predicate(new FileInfo("foo.html")));
            Assert.AreEqual(false, predicate(new FileInfo("bar.html")));

            Assert.AreEqual(false, (await pipeline(new Uri("http://example.com/foo")))(new FileInfo("foo.html")));
        }

        [TestMethod]
        public async Task Usage_IMiddleware() {
            var middlewares = new PredicateMiddleware<Uri, FileInfo>[]{
                new HttpsPredicate(),
                new StaticFilePredicate(),
            };
            Func<Uri, Task<PredicateFunc<FileInfo>>> pipeline = FilterPipeline.Build(middlewares.Select(x => x.ToDelegate()));

            var predicate = await pipeline(new Uri("https://example.com/foo"));
            Assert.AreEqual(true, predicate(new FileInfo("foo.html")));
            Assert.AreEqual(false, predicate(new FileInfo("bar.html")));

            Assert.AreEqual(false, (await pipeline(new Uri("http://example.com/foo")))(new FileInfo("foo.html")));
        }

        public class HttpsPredicate : PredicateMiddleware<Uri, FileInfo> {

            protected override async Task<PredicateFunc<FileInfo>> CreateAsync(Uri context, Func<Uri, Task<PredicateFunc<FileInfo>>> next) {
                if (!context.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)) { return _ => false; }
                return await next(context);
            }
        }

        public class StaticFilePredicate : PredicateMiddleware<Uri, FileInfo> {

            protected override PredicateFunc<FileInfo> Create(Uri context, ref bool cancelled) {
                var uriPath = context.AbsolutePath.TrimStart('/');
                return file => Path.GetFileNameWithoutExtension(file.Name).Equals(uriPath, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
