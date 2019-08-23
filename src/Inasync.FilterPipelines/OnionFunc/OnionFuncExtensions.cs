using System;

namespace Inasync.OnionFunc {

    /// <summary>
    /// <see cref="Func{T, TResult}"/> を他の <see cref="Func{T, TResult}"/> でラップしてパイプライン化する為のヘルパークラス。
    /// </summary>
    public static class OnionFuncExtensions {

        /// <summary>
        /// 関数をミドルウェア コンポーネントでラップします。
        /// </summary>
        /// <typeparam name="T">関数のパラメーターの型。</typeparam>
        /// <typeparam name="TResult">関数の戻り値の型。</typeparam>
        /// <param name="onionFunc">ラップ対象の関数。</param>
        /// <param name="middleware">関数をラップするミドルウェア コンポーネント。</param>
        /// <returns>ミドルウェア コンポーネントでラップされた新しい関数。常に非 <c>null</c>。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="onionFunc"/> or <paramref name="middleware"/> is <c>null</c>.</exception>
        public static Func<T, TResult> Wear<T, TResult>(this Func<T, TResult> onionFunc, MiddlewareFunc<T, TResult> middleware) {
            if (onionFunc == null) { throw new ArgumentNullException(nameof(onionFunc)); }
            if (middleware == null) { throw new ArgumentNullException(nameof(middleware)); }

            return middleware(onionFunc);
        }
    }
}
