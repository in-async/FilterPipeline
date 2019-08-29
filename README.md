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
```

## Licence
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
