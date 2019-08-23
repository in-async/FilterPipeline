using System;
using System.Collections.Generic;
using System.Linq;
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
        /// <param name="filters"><see cref="PredicateFilterMiddleware{T, TContext}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="filters"/> が空の場合は、<see cref="PredicateFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="filters"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<Func<T, bool>>> Build<T, TContext>(IEnumerable<PredicateFilterMiddleware<T, TContext>> filters) {
            if (filters == null) { throw new ArgumentNullException(nameof(filters)); }

            return OnionPipeline.Build(
                  middlewares: filters.Select(filter => new MiddlewareFunc<TContext, Task<Func<T, bool>>>(filter))
                , handler: _ => Task.FromResult(PredicateFilter<T, TContext>.NullFilter)
            );
        }

        /// <summary>
        /// <typeparamref name="T"/> のシーケンスを処理するフィルター パイプラインを構築します。
        /// </summary>
        /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <param name="filters"><see cref="SequenceFilterMiddleware{T, TContext}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="filters"/> が空の場合は、<see cref="SequenceFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="filters"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<SequenceFilterFunc<T>>> Build<T, TContext>(IEnumerable<SequenceFilterMiddleware<T, TContext>> filters) {
            if (filters == null) { throw new ArgumentNullException(nameof(filters)); }

            return OnionPipeline.Build(
                  middlewares: filters.Select(filter => new MiddlewareFunc<TContext, Task<SequenceFilterFunc<T>>>(filter))
                , handler: _ => Task.FromResult(SequenceFilter<T, TContext>.NullFilter)
            );
        }
    }
}
