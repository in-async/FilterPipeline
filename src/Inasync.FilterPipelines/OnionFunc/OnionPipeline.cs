using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Inasync.OnionFunc {

    /// <summary>
    /// オニオン パイプラインに関するヘルパー クラス。
    /// </summary>
    public static class OnionPipeline {

        /// <summary>
        /// ハンドラー コンポーネントをラッピングするオニオン パイプラインを構築します。
        /// </summary>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <typeparam name="TResult">パイプラインの実行結果の型。</typeparam>
        /// <param name="middlewares"><see cref="IMiddleware{TContext, TResult}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <param name="handler">パイプラインの終端、オニオンの中心に配置されるハンドラー デリゲート。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<paramref name="handler"/> をそのまま返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="middlewares"/> or <paramref name="handler"/> is <c>null</c>.</exception>
        public static Func<TContext, TResult> Build<TContext, TResult>(IEnumerable<IMiddleware<TContext, TResult>> middlewares, Func<TContext, TResult> handler) {
            if (middlewares == null) { throw new ArgumentNullException(nameof(middlewares)); }
            if (handler == null) { throw new ArgumentNullException(nameof(handler)); }

            var pipeline = handler;
            foreach (var middleware in middlewares.Reverse()) {
                Debug.Assert(middleware != null);

                pipeline = pipeline.Wrap(middleware);
            }
            return pipeline;
        }

        /// <summary>
        /// ハンドラー コンポーネントをラッピングするオニオン パイプラインを構築します。
        /// </summary>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <typeparam name="TResult">パイプラインの実行結果の型。</typeparam>
        /// <param name="middlewares"><see cref="MiddlewareFunc{TContext, TResult}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <param name="handler">パイプラインの終端、オニオンの中心に配置されるハンドラー デリゲート。</param>
        /// <returns>
        /// パイプラインのエントリーポイントとなるデリゲート。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<paramref name="handler"/> をそのまま返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="middlewares"/> or <paramref name="handler"/> is <c>null</c>.</exception>
        public static Func<TContext, TResult> Build<TContext, TResult>(IEnumerable<MiddlewareFunc<TContext, TResult>> middlewares, Func<TContext, TResult> handler) {
            if (middlewares == null) { throw new ArgumentNullException(nameof(middlewares)); }
            if (handler == null) { throw new ArgumentNullException(nameof(handler)); }

            var pipeline = handler;
            foreach (var middleware in middlewares.Reverse()) {
                Debug.Assert(middleware != null);

                pipeline = pipeline.Wrap(middleware);
            }
            return pipeline;
        }
    }
}
