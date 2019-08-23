using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Inasync.OnionFunc;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// フィルター パイプラインに関するヘルパー クラス。
    /// </summary>
    public static class FilterPipeline {

        /// <summary>
        /// <typeparamref name="T"/> の述語処理を行うフィルター パイプラインを構築します。
        /// </summary>
        /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <param name="middlewares"><see cref="PredicateFilterMiddleware{T, TContext}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<see cref="PredicateFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="middlewares"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<PredicateFilterFunc<T>>> Build<T, TContext>(IEnumerable<MiddlewareFunc<TContext, Task<PredicateFilterFunc<T>>>> middlewares) {
            if (middlewares == null) { throw new ArgumentNullException(nameof(middlewares)); }

            return OnionPipeline.Build(middlewares, handler: _ => Task.FromResult(PredicateFilter<T, TContext>.NullFilter));
        }

        /// <summary>
        /// <typeparamref name="T"/> のシーケンスを処理するフィルター パイプラインを構築します。
        /// </summary>
        /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <param name="middlewares"><see cref="SequenceFilterMiddleware{T, TContext}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<see cref="SequenceFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="middlewares"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<SequenceFilterFunc<T>>> Build<T, TContext>(IEnumerable<MiddlewareFunc<TContext, Task<SequenceFilterFunc<T>>>> middlewares) {
            if (middlewares == null) { throw new ArgumentNullException(nameof(middlewares)); }

            return OnionPipeline.Build(middlewares, handler: _ => Task.FromResult(SequenceFilter<T, TContext>.NullFilter));
        }
    }
}
