using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// フィルター パイプラインを構成するシーケンス フィルターのインターフェース。
    /// </summary>
    /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    public interface ISequenceFilter<T, TContext> {

        /// <summary>
        /// ミドルウェアで定義されている処理を組み込んだ新しいフィルター パイプライン関数を作成します。
        /// </summary>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>このミドルウェアを組み込んだ新しいフィルター パイプライン関数。常に非 <c>null</c>。</returns>
        Func<TContext, Task<SequenceFilterFunc<T>>> Middleware(Func<TContext, Task<SequenceFilterFunc<T>>> next);
    }

    /// <summary>
    /// <see cref="ISequenceFilter{T, TContext}"/> の拡張クラス。
    /// </summary>
    public static class SequenceFilterExtensions {

        /// <summary>
        /// フィルターを <see cref="MiddlewareFunc{T, TResult}"/> デリゲートに変換します。
        /// </summary>
        /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <param name="filter">ミドルウェアに変換する <see cref="ISequenceFilter{T, TContext}"/>。</param>
        /// <returns>フィルターから変換された <see cref="MiddlewareFunc{T, TResult}"/> デリゲート。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <c>null</c>.</exception>
        public static MiddlewareFunc<TContext, Task<SequenceFilterFunc<T>>> ToMiddleware<T, TContext>(this ISequenceFilter<T, TContext> filter) {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }

            return new MiddlewareFunc<TContext, Task<SequenceFilterFunc<T>>>(filter.Middleware);
        }
    }

    /// <summary>
    /// <typeparamref name="T"/> のシーケンスをフィルター処理するデリゲート。
    /// </summary>
    /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
    /// <param name="source">フィルター処理の対象となる <typeparamref name="T"/> のシーケンス。常に非 <c>null</c>。</param>
    /// <returns>フィルター処理された <typeparamref name="T"/> のシーケンス。常に非 <c>null</c>。</returns>
    public delegate IEnumerable<T> SequenceFilterFunc<T>(IEnumerable<T> source);

    /// <summary>
    /// <see cref="ISequenceFilter{T, TContext}"/> の抽象クラス。
    /// 既定の実装ではフィルター処理を行いません。
    /// </summary>
    public abstract class SequenceFilter<T, TContext> : ISequenceFilter<T, TContext> {

        /// <summary>
        /// シーケンスを素通しするフィルター デリゲート。
        /// </summary>
        public static readonly SequenceFilterFunc<T> NullFilter = source => source;

        /// <summary>
        /// <see cref="ISequenceFilter{T, TContext}.Middleware(Func{TContext, Task{SequenceFilterFunc{T}}})"/> の実装。
        /// 既定の実装では <see cref="CreateAsync(TContext, Func{TContext, Task{SequenceFilterFunc{T}}})"/> に処理を委譲します。
        /// </summary>
        public Func<TContext, Task<SequenceFilterFunc<T>>> Middleware(Func<TContext, Task<SequenceFilterFunc<T>>> next) => context => CreateAsync(context, next);

        /// <summary>
        /// <see cref="SequenceFilterFunc{T}"/> を作成します。
        /// 既定の実装では <see cref="Create(TContext, ref bool)"/> に処理を委譲します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>このフィルター コンポーネント以降の処理を表す <see cref="SequenceFilterFunc{T}"/>。常に非 <c>null</c>。</returns>
        protected virtual Task<SequenceFilterFunc<T>> CreateAsync(TContext context, Func<TContext, Task<SequenceFilterFunc<T>>> next) {
            Debug.Assert(context != null);
            Debug.Assert(next != null);

            var cancelled = false;
            SequenceFilterFunc<T> filterFunc = Create(context, ref cancelled);
            Debug.Assert(filterFunc != null);
            if (cancelled) { return Task.FromResult(filterFunc); }

            if (filterFunc == NullFilter) { return next(context); }
            return InternalAsync();

            async Task<SequenceFilterFunc<T>> InternalAsync() {
                SequenceFilterFunc<T> nextFunc = await next(context).ConfigureAwait(false);
                Debug.Assert(nextFunc != null);
                if (nextFunc == NullFilter) { return filterFunc; }

                return source => nextFunc(filterFunc(source));
            }
        }

        /// <summary>
        /// <see cref="SequenceFilterFunc{T}"/> を作成します。
        /// 既定の実装では <see cref="NullFilter"/> を返します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="cancelled">パイプラインの残りのコンポーネントをショートサーキットする場合は <c>true</c>、それ以外は <c>false</c>。既定値は <c>false</c>。</param>
        /// <returns>このフィルター コンポーネント以降の処理を表す <see cref="SequenceFilterFunc{T}"/>。常に非 <c>null</c>。</returns>
        protected virtual SequenceFilterFunc<T> Create(TContext context, ref bool cancelled) => NullFilter;
    }
}
