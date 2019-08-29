using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// フィルター パイプラインを構成するシーケンス フィルターのインターフェース。
    /// </summary>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    /// <typeparam name="TEntity">フィルター処理の対象となる要素の型。</typeparam>
    public interface ISequenceFilter<TContext, TEntity> {

        /// <summary>
        /// ミドルウェアで定義されている処理を組み込んだ新しいフィルター パイプライン関数を作成します。
        /// </summary>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>このミドルウェアを組み込んだ新しいフィルター パイプライン関数。常に非 <c>null</c>。</returns>
        Func<TContext, Task<SequenceFilterFunc<TEntity>>> Middleware(Func<TContext, Task<SequenceFilterFunc<TEntity>>> next);
    }

    /// <summary>
    /// <see cref="ISequenceFilter{TContext, TEntity}"/> の拡張クラス。
    /// </summary>
    public static class SequenceFilterExtensions {

        /// <summary>
        /// フィルターを <see cref="MiddlewareFunc{T, TResult}"/> デリゲートに変換します。
        /// </summary>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <typeparam name="TEntity">フィルター処理の対象となる要素の型。</typeparam>
        /// <param name="filter">ミドルウェアに変換する <see cref="ISequenceFilter{TContext, TEntity}"/>。</param>
        /// <returns>フィルターから変換された <see cref="MiddlewareFunc{T, TResult}"/> デリゲート。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <c>null</c>.</exception>
        public static MiddlewareFunc<TContext, Task<SequenceFilterFunc<TEntity>>> ToMiddleware<TContext, TEntity>(this ISequenceFilter<TContext, TEntity> filter) {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }

            return new MiddlewareFunc<TContext, Task<SequenceFilterFunc<TEntity>>>(filter.Middleware);
        }
    }

    /// <summary>
    /// <typeparamref name="TEntity"/> のシーケンスをフィルター処理するデリゲート。
    /// </summary>
    /// <typeparam name="TEntity">フィルター処理の対象となる要素の型。</typeparam>
    /// <param name="source">フィルター処理の対象となる <typeparamref name="TEntity"/> のシーケンス。常に非 <c>null</c>。</param>
    /// <returns>フィルター処理された <typeparamref name="TEntity"/> のシーケンス。常に非 <c>null</c>。</returns>
    public delegate IEnumerable<TEntity> SequenceFilterFunc<TEntity>(IEnumerable<TEntity> source);

    /// <summary>
    /// <see cref="ISequenceFilter{TContext, TEntity}"/> の抽象クラス。
    /// 既定の実装ではフィルター処理を行いません。
    /// </summary>
    public abstract class SequenceFilter<TContext, TEntity> : ISequenceFilter<TContext, TEntity> {

        /// <summary>
        /// シーケンスを素通しするフィルター デリゲート。
        /// </summary>
        public static readonly SequenceFilterFunc<TEntity> NullFilter = source => source;

        /// <summary>
        /// <see cref="ISequenceFilter{TContext, TEntity}.Middleware(Func{TContext, Task{SequenceFilterFunc{TEntity}}})"/> の実装。
        /// 既定の実装では <see cref="CreateAsync(TContext, Func{TContext, Task{SequenceFilterFunc{TEntity}}})"/> に処理を委譲します。
        /// </summary>
        public Func<TContext, Task<SequenceFilterFunc<TEntity>>> Middleware(Func<TContext, Task<SequenceFilterFunc<TEntity>>> next) => context => CreateAsync(context, next);

        /// <summary>
        /// <see cref="SequenceFilterFunc{TEntity}"/> を作成します。
        /// 既定の実装では <see cref="Create(TContext, ref bool)"/> に処理を委譲します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>このフィルター コンポーネント以降の処理を表す <see cref="SequenceFilterFunc{TEntity}"/>。常に非 <c>null</c>。</returns>
        protected virtual Task<SequenceFilterFunc<TEntity>> CreateAsync(TContext context, Func<TContext, Task<SequenceFilterFunc<TEntity>>> next) {
            Debug.Assert(context != null);
            Debug.Assert(next != null);

            var cancelled = false;
            SequenceFilterFunc<TEntity> filterFunc = Create(context, ref cancelled);
            Debug.Assert(filterFunc != null);
            if (cancelled) { return Task.FromResult(filterFunc); }

            if (filterFunc == NullFilter) { return next(context); }
            return InternalAsync();

            async Task<SequenceFilterFunc<TEntity>> InternalAsync() {
                SequenceFilterFunc<TEntity> nextFunc = await next(context).ConfigureAwait(false);
                Debug.Assert(nextFunc != null);
                if (nextFunc == NullFilter) { return filterFunc; }

                return source => nextFunc(filterFunc(source));
            }
        }

        /// <summary>
        /// <see cref="SequenceFilterFunc{TEntity}"/> を作成します。
        /// 既定の実装では <see cref="NullFilter"/> を返します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="cancelled">パイプラインの残りのコンポーネントをショートサーキットする場合は <c>true</c>、それ以外は <c>false</c>。既定値は <c>false</c>。</param>
        /// <returns>このフィルター コンポーネント以降の処理を表す <see cref="SequenceFilterFunc{TEntity}"/>。常に非 <c>null</c>。</returns>
        protected virtual SequenceFilterFunc<TEntity> Create(TContext context, ref bool cancelled) => NullFilter;
    }
}
