using System;

namespace Inasync.OnionFunc {

    /// <summary>
    /// <see cref="Func{T, TResult}"/> を他の <see cref="Func{T, TResult}"/> でラップする拡張メソッドの定義クラス。
    /// </summary>
    public static class OnionFuncExtensions {

        /// <summary>
        /// 元となるデリゲートを、同じシグネチャを持つミドルウェアでラップします。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="onionFunc">ラップされる <see cref="Func{T, TResult}"/> デリゲート。</param>
        /// <param name="middleware"><paramref name="onionFunc"/> をラップする <see cref="Func{T, TResult}"/> デリゲート。</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="onionFunc"/> or <paramref name="middleware"/> is <c>null</c>.</exception>
        public static Func<T, TResult> Wrap<T, TResult>(this Func<T, TResult> onionFunc, Func<Func<T, TResult>, Func<T, TResult>> middleware) {
            if (onionFunc == null) { throw new ArgumentNullException(nameof(onionFunc)); }
            if (middleware == null) { throw new ArgumentNullException(nameof(middleware)); }

            return middleware(onionFunc);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="onionFunc"></param>
        /// <param name="middleware"></param>
        /// <exception cref="ArgumentNullException"><paramref name="onionFunc"/> or <paramref name="middleware"/> is <c>null</c>.</exception>
        public static Func<T, TResult> Wrap<T, TResult>(this Func<T, TResult> onionFunc, MiddlewareFunc<T, TResult> middleware) {
            if (onionFunc == null) { throw new ArgumentNullException(nameof(onionFunc)); }
            if (middleware == null) { throw new ArgumentNullException(nameof(middleware)); }

            return context => middleware(context, onionFunc);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="onionFunc"></param>
        /// <param name="middleware"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="onionFunc"/> or <paramref name="middleware"/> is <c>null</c>.</exception>
        public static Func<T, TResult> Wrap<T, TResult>(this Func<T, TResult> onionFunc, IMiddleware<T, TResult> middleware) {
            if (onionFunc == null) { throw new ArgumentNullException(nameof(onionFunc)); }
            if (middleware == null) { throw new ArgumentNullException(nameof(middleware)); }

            return context => middleware.Invoke(context, onionFunc);
        }
    }
}
