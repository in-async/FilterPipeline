using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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
        /// <param name="middlewares">固有の <see cref="PredicateFilterFunc{T}"/> を作成するミドルウェアのコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<see cref="PredicateFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="middlewares"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<PredicateFilterFunc<T>>> Build<T, TContext>(IEnumerable<MiddlewareFunc<TContext, Task<PredicateFilterFunc<T>>>> middlewares) {
            if (middlewares == null) { throw new ArgumentNullException(nameof(middlewares)); }

            return Build(middlewares, handler: _ => Task.FromResult(PredicateFilter<T, TContext>.NullFilter));
        }

        /// <summary>
        /// <typeparamref name="T"/> のシーケンスを処理するフィルター パイプラインを構築します。
        /// </summary>
        /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <param name="middlewares">固有の <see cref="SequenceFilterFunc{T}"/> を作成するミドルウェアのコレクション。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<see cref="SequenceFilter{T, TContext}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="middlewares"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<SequenceFilterFunc<T>>> Build<T, TContext>(IEnumerable<MiddlewareFunc<TContext, Task<SequenceFilterFunc<T>>>> middlewares) {
            if (middlewares == null) { throw new ArgumentNullException(nameof(middlewares)); }

            return Build(middlewares, handler: _ => Task.FromResult(SequenceFilter<T, TContext>.NullFilter));
        }

        /// <summary>
        /// ハンドラー コンポーネントをコアとするオニオン パイプライン関数を構築します。
        /// </summary>
        /// <typeparam name="T">パイプラインのパラメーターの型。</typeparam>
        /// <typeparam name="TResult">パイプラインの戻り値の型。</typeparam>
        /// <param name="middlewares"><see cref="MiddlewareFunc{TContext, TResult}"/> のコレクション。常に非 <c>null</c>。要素は常に非 <c>null</c>。</param>
        /// <param name="handler">パイプラインの終端、オニオンの中心に配置されるハンドラー コンポーネント。常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリー関数。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<paramref name="handler"/> をそのまま返します。
        /// </returns>
        private static Func<T, TResult> Build<T, TResult>(IEnumerable<MiddlewareFunc<T, TResult>> middlewares, Func<T, TResult> handler) {
            Debug.Assert(middlewares != null);
            Debug.Assert(handler != null);

            var pipeline = handler;
            foreach (var middleware in middlewares.Reverse()) {
                Debug.Assert(middleware != null);

                pipeline = pipeline.Wear(middleware);
            }

            return pipeline;
        }
    }
}
