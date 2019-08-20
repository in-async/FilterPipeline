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
Func<object, Task<Func<int, bool>>> pipeline = FilterPipeline.Build(new PredicateFilterCreator<int, object>[]{
    async (context, next) => {
        var nextFunc = await next();
        return num => (num % 4 == 0) && nextFunc(num);
    },
    async (context, next) => {
        var nextFunc = await next();
        return num => (num % 3 == 0) && nextFunc(num);
    },
});
var pipelineFunc = await pipeline(new object());

Assert.AreEqual(true, pipelineFunc(24));
Assert.AreEqual(false, pipelineFunc(30));
```

## Licence
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details
