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
        /// <typeparamref name="TEntity"/> の述語パイプラインを構築します。
        /// </summary>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <typeparam name="TEntity">述語処理の対象となるエンティティの型。</typeparam>
        /// <param name="middlewares">固有の <see cref="PredicateFunc{TEntity}"/> を作成するミドルウェアのシーケンス。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリー関数。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<see cref="PredicateMiddleware{TContext, TEntity}.NullPredicate"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="middlewares"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<PredicateFunc<TEntity>>> Build<TContext, TEntity>(IEnumerable<MiddlewareFunc<TContext, Task<PredicateFunc<TEntity>>>> middlewares) {
            if (middlewares == null) { throw new ArgumentNullException(nameof(middlewares)); }

            return Build(middlewares, handler: _ => Task.FromResult(PredicateMiddleware<TContext, TEntity>.NullPredicate));
        }

        /// <summary>
        /// <typeparamref name="TEntity"/> のフィルター パイプラインを構築します。
        /// </summary>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <typeparam name="TEntity">フィルター処理の対象となるエンティティの型。</typeparam>
        /// <param name="middlewares">固有の <see cref="FilterFunc{TEntity}"/> を作成するミドルウェアのシーケンス。要素は常に非 <c>null</c>。</param>
        /// <returns>
        /// パイプラインのエントリー関数。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<see cref="FilterMiddleware{TContext, TEntity}.NullFilter"/> を返すデリゲートを返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="middlewares"/> is <c>null</c>.</exception>
        public static Func<TContext, Task<FilterFunc<TEntity>>> Build<TContext, TEntity>(IEnumerable<MiddlewareFunc<TContext, Task<FilterFunc<TEntity>>>> middlewares) {
            if (middlewares == null) { throw new ArgumentNullException(nameof(middlewares)); }

            return Build(middlewares, handler: _ => Task.FromResult(FilterMiddleware<TContext, TEntity>.NullFilter));
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

                pipeline = pipeline.Layer(middleware);
            }

            return pipeline;
        }
    }
}
