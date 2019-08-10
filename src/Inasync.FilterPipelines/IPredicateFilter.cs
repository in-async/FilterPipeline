using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// フィルター パイプラインを構成する述語フィルターのインターフェース。
    /// </summary>
    /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    public interface IPredicateFilter<T, in TContext> {

        /// <summary>
        /// <typeparamref name="T"/> の述語フィルター デリゲートを作成します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>
        /// このフィルター コンポーネント以降の処理を表す述語フィルター デリゲート。常に非 <c>null</c>。
        /// デリゲートのパラメーターも常に非 <c>null</c>。
        /// </returns>
        Task<Func<T, bool>> CreateAsync(TContext context, Func<Task<Func<T, bool>>> next);
    }

    /// <summary>
    /// <typeparamref name="T"/> の述語フィルター デリゲートを作成します。
    /// </summary>
    /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
    /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
    /// <returns>
    /// このフィルター コンポーネント以降の処理を表す述語フィルター デリゲート。常に非 <c>null</c>。
    /// デリゲートのパラメーターも常に非 <c>null</c>。
    /// </returns>
    public delegate Task<Func<T, bool>> PredicateFilterCreator<T, in TContext>(TContext context, Func<Task<Func<T, bool>>> next);

    /// <summary>
    /// <see cref="IPredicateFilter{T, TContext}"/> の抽象クラス。
    /// 既定の実装ではフィルター処理を行いません。
    /// </summary>
    public abstract class PredicateFilter<T, TContext> : IPredicateFilter<T, TContext> {

        /// <summary>
        /// 無条件に <c>true</c> を返す述語フィルター デリゲート。
        /// </summary>
        public static readonly Func<T, bool> NullFilter = _ => true;

        /// <summary>
        /// <see cref="IPredicateFilter{T, TContext}.CreateAsync(TContext, Func{Task{Func{T, bool}}})"/> の実装。
        /// 既定の実装では <see cref="Create(TContext, ref bool)"/> に処理を委譲します。
        /// </summary>
        public virtual Task<Func<T, bool>> CreateAsync(TContext context, Func<Task<Func<T, bool>>> next) {
            Debug.Assert(context != null);
            Debug.Assert(next != null);

            var cancelled = false;
            Func<T, bool> filterFunc = Create(context, ref cancelled);
            Debug.Assert(filterFunc != null);
            if (cancelled) { return Task.FromResult(filterFunc); }

            if (filterFunc == NullFilter) { return next(); }
            return InternalAsync();

            async Task<Func<T, bool>> InternalAsync() {
                Func<T, bool> nextFunc = await next().ConfigureAwait(false);
                Debug.Assert(nextFunc != null);
                if (nextFunc == NullFilter) { return filterFunc; }

                return x => filterFunc(x) && nextFunc(x);
            }
        }

        /// <summary>
        /// <typeparamref name="T"/> の述語フィルター デリゲートを作成します。
        /// 既定の実装では <see cref="NullFilter"/> を返します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="cancelled">パイプラインの残りのコンポーネントをショートサーキットする場合は <c>true</c>、それ以外は <c>false</c>。既定値は <c>false</c>。</param>
        /// <returns>
        /// このフィルター コンポーネント以降の処理を表す述語フィルター デリゲート。常に非 <c>null</c>。
        /// デリゲートのパラメーターも常に非 <c>null</c>。
        /// </returns>
        protected virtual Func<T, bool> Create(TContext context, ref bool cancelled) => NullFilter;
    }
}
