using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// パイプラインを構成するフィルター ミドルウェアのインターフェース。
    /// </summary>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    /// <typeparam name="TEntity">フィルター処理の対象となるエンティティの型。</typeparam>
    public interface IFilterMiddleware<TContext, TEntity> : IMiddleware<TContext, Task<FilterFunc<TEntity>>> { }

    /// <summary>
    /// <typeparamref name="TEntity"/> のフィルター デリゲート。
    /// </summary>
    /// <typeparam name="TEntity">フィルター処理の対象となるエンティティの型。</typeparam>
    /// <param name="source">フィルター処理の対象となる <typeparamref name="TEntity"/> のシーケンス。常に非 <c>null</c>。</param>
    /// <returns>フィルター処理された <typeparamref name="TEntity"/> のシーケンス。常に非 <c>null</c>。</returns>
    public delegate IEnumerable<TEntity> FilterFunc<TEntity>(IEnumerable<TEntity> source);

    /// <summary>
    /// <see cref="IFilterMiddleware{TContext, TEntity}"/> の抽象クラス。
    /// 既定の実装ではフィルターは <see cref="NullFilter"/> です。
    /// </summary>
    public abstract class FilterMiddleware<TContext, TEntity> : IFilterMiddleware<TContext, TEntity> {

        /// <summary>
        /// シーケンスを素通しするフィルター デリゲート。
        /// </summary>
        public static readonly FilterFunc<TEntity> NullFilter = source => source;

        /// <summary>
        /// <see cref="IMiddleware{T, TResult}.Invoke(Func{T, TResult})"/> の実装。
        /// 既定の実装では <see cref="CreateAsync(TContext, Func{TContext, Task{FilterFunc{TEntity}}})"/> に処理を委譲します。
        /// </summary>
        public virtual Func<TContext, Task<FilterFunc<TEntity>>> Invoke(Func<TContext, Task<FilterFunc<TEntity>>> next) => context => CreateAsync(context, next);

        /// <summary>
        /// <see cref="FilterFunc{TEntity}"/> を作成します。
        /// 既定の実装では <see cref="Create(TContext, ref bool)"/> に処理を委譲します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>このコンポーネント以降のフィルター処理を表すデリゲート。常に非 <c>null</c>。</returns>
        protected virtual Task<FilterFunc<TEntity>> CreateAsync(TContext context, Func<TContext, Task<FilterFunc<TEntity>>> next) {
            Debug.Assert(context != null);
            Debug.Assert(next != null);

            var cancelled = false;
            FilterFunc<TEntity> filter = Create(context, ref cancelled);
            Debug.Assert(filter != null);
            if (cancelled) { return Task.FromResult(filter); }

            if (filter == NullFilter) { return next(context); }
            return InternalAsync();

            async Task<FilterFunc<TEntity>> InternalAsync() {
                FilterFunc<TEntity> nextFilter = await next(context).ConfigureAwait(false);
                Debug.Assert(nextFilter != null);
                if (nextFilter == NullFilter) { return filter; }

                return source => nextFilter(filter(source));
            }
        }

        /// <summary>
        /// <see cref="FilterFunc{TEntity}"/> を作成します。
        /// 既定の実装では <see cref="NullFilter"/> を返します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="cancelled">パイプラインの残りのコンポーネントをショートサーキットする場合は <c>true</c>、それ以外は <c>false</c>。既定値は <c>false</c>。</param>
        /// <returns>このコンポーネント以降のフィルター処理を表すデリゲート。常に非 <c>null</c>。</returns>
        protected virtual FilterFunc<TEntity> Create(TContext context, ref bool cancelled) => NullFilter;
    }
}
