using System;

namespace Inasync {

    /// <summary>
    /// パイプラインを構成するミドルウェア コンポーネントのインターフェース。
    /// </summary>
    /// <typeparam name="T">パイプラインの実行時パラメーターの型。</typeparam>
    /// <typeparam name="TResult">パイプラインの実行結果の型。</typeparam>
    public interface IMiddleware<T, TResult> {

        /// <summary>
        /// ミドルウェアで定義されている処理を組み込んだ新しいパイプライン関数を作成します。
        /// </summary>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>ミドルウェアの処理を組み込んだ新しいパイプライン関数。常に非 <c>null</c>。</returns>
        Func<T, TResult> Invoke(Func<T, TResult> next);
    }

    /// <summary>
    /// <see cref="IMiddleware{T, TResult}"/> の拡張クラス。
    /// </summary>
    public static class MiddlewareExtensions {

        /// <summary>
        /// ミドルウェア コンポーネントを <see cref="MiddlewareFunc{T, TResult}"/> デリゲートに変換します。
        /// </summary>
        /// <typeparam name="T">パイプラインの実行時パラメーターの型。</typeparam>
        /// <typeparam name="TResult">パイプラインの実行結果の型。</typeparam>
        /// <param name="middleware">デリゲートに変換する <see cref="IMiddleware{T, TResult}"/>。</param>
        /// <returns>コンポーネントから変換された <see cref="MiddlewareFunc{T, TResult}"/> デリゲート。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="middleware"/> is <c>null</c>.</exception>
        public static MiddlewareFunc<T, TResult> ToDelegate<T, TResult>(this IMiddleware<T, TResult> middleware) {
            if (middleware == null) { throw new ArgumentNullException(nameof(middleware)); }

            return new MiddlewareFunc<T, TResult>(middleware.Invoke);
        }
    }
}
