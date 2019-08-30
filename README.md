# FilterPipelines
[![Build status](https://ci.appveyor.com/api/projects/status/o9qub1ba5r3qx7tj/branch/master?svg=true)](https://ci.appveyor.com/project/inasync/filterpipelines/branch/master)
[![NuGet](https://img.shields.io/nuget/v/Inasync.FilterPipelines.svg)](https://www.nuget.org/packages/Inasync.FilterPipelines/)

***FilterPipelines*** は middleware pattern によってフィルター パイプラインを構築する為の、シンプルな .NET ヘルパーライブラリです。

## Target Frameworks
- .NET Standard 2.0+
- .NET Standard 1.0+
- .NET Framework 4.5+


## Usage
```cs
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
```

## Licence
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
