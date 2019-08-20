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
        /// <param name="filters"><see cref="IPredicateFilter{T, TContext}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="filters"/> が空の場合は、<see cref="PredicateFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="filters"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<Func<T, bool>>> Build<T, TContext>(IEnumerable<IPredicateFilter<T, TContext>> filters) {
            if (filters == null) { throw new ArgumentNullException(nameof(filters)); }

            return Build(filters.Select(filter => new PredicateFilterCreator<T, TContext>(filter.CreateAsync)));
        }

        /// <summary>
        /// <typeparamref name="T"/> の述語処理を行うフィルター パイプラインを構築します。
        /// </summary>
        /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <param name="filters"><see cref="PredicateFilterCreator{T, TContext}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="filters"/> が空の場合は、<see cref="PredicateFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="filters"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<Func<T, bool>>> Build<T, TContext>(IEnumerable<PredicateFilterCreator<T, TContext>> filters) {
            if (filters == null) { throw new ArgumentNullException(nameof(filters)); }

            Func<TContext, Task<Func<T, bool>>> pipeline = _ => Task.FromResult(PredicateFilter<T, TContext>.NullFilter);
            foreach (var filter in filters.Reverse()) {
                pipeline = pipeline.Wear(next => context => filter(context, () => next(context)));
            }

            return pipeline;
        }

        /// <summary>
        /// <typeparamref name="T"/> のシーケンスを処理するフィルター パイプラインを構築します。
        /// </summary>
        /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <param name="filters"><see cref="ISequenceFilter{T, TContext}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="filters"/> が空の場合は、<see cref="SequenceFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="filters"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<SequenceFilterFunc<T>>> Build<T, TContext>(IEnumerable<ISequenceFilter<T, TContext>> filters) {
            if (filters == null) { throw new ArgumentNullException(nameof(filters)); }

            return Build(filters.Select(filter => new SequenceFilterCreator<T, TContext>(filter.CreateAsync)));
        }

        /// <summary>
        /// <typeparamref name="T"/> のシーケンスを処理するフィルター パイプラインを構築します。
        /// </summary>
        /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <param name="filters"><see cref="SequenceFilterCreator{T, TContext}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="filters"/> が空の場合は、<see cref="SequenceFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="filters"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<SequenceFilterFunc<T>>> Build<T, TContext>(IEnumerable<SequenceFilterCreator<T, TContext>> filters) {
            if (filters == null) { throw new ArgumentNullException(nameof(filters)); }

            Func<TContext, Task<SequenceFilterFunc<T>>> pipeline = _ => Task.FromResult(SequenceFilter<T, TContext>.NullFilter);
            foreach (var filter in filters.Reverse()) {
                pipeline = pipeline.Wear(next => context => filter(context, () => next(context)));
            }

            return pipeline;
        }
    }
}
