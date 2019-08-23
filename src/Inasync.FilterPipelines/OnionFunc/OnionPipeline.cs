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
        /// ハンドラー コンポーネントをコアとするオニオン パイプライン関数を構築します。
        /// </summary>
        /// <typeparam name="T">パイプラインのパラメーターの型。</typeparam>
        /// <typeparam name="TResult">パイプラインの戻り値の型。</typeparam>
        /// <param name="middlewares"><see cref="MiddlewareFunc{TContext, TResult}"/> のコレクション。要素は常に非 <c>null</c>。</param>
        /// <param name="handler">パイプラインの終端、オニオンの中心に配置されるハンドラー コンポーネント。</param>
        /// <returns>
        /// パイプラインのエントリー関数。常に非 <c>null</c>。
        /// <paramref name="middlewares"/> が空の場合は、<paramref name="handler"/> をそのまま返します。
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="middlewares"/> or <paramref name="handler"/> is <c>null</c>.</exception>
        public static Func<T, TResult> Build<T, TResult>(IEnumerable<MiddlewareFunc<T, TResult>> middlewares, Func<T, TResult> handler) {
            if (middlewares == null) { throw new ArgumentNullException(nameof(middlewares)); }
            if (handler == null) { throw new ArgumentNullException(nameof(handler)); }

            var pipeline = handler;
            foreach (var middleware in middlewares.Reverse()) {
                Debug.Assert(middleware != null);

                pipeline = pipeline.Wear(middleware);
            }

            return pipeline;
        }
    }
}
