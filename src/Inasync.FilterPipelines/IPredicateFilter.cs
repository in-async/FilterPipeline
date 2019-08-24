using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// フィルター パイプラインを構成する述語フィルターのインターフェース。
    /// </summary>
    /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    public interface IPredicateFilter<T, TContext> {

        /// <summary>
        /// ミドルウェアで定義されている処理を組み込んだ新しいフィルター パイプライン関数を作成します。
        /// </summary>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>このミドルウェアを組み込んだ新しいフィルター パイプライン関数。常に非 <c>null</c>。</returns>
        Func<TContext, Task<PredicateFilterFunc<T>>> Middleware(Func<TContext, Task<PredicateFilterFunc<T>>> next);
    }

    /// <summary>
    /// <see cref="IPredicateFilter{T, TContext}"/> の拡張クラス。
    /// </summary>
    public static class PredicateFilterExtensions {

        /// <summary>
        /// フィルターを <see cref="MiddlewareFunc{T, TResult}"/> デリゲートに変換します。
        /// </summary>
        /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
        /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
        /// <param name="filter">ミドルウェアに変換する <see cref="IPredicateFilter{T, TContext}"/>。</param>
        /// <returns>フィルターから変換された <see cref="MiddlewareFunc{T, TResult}"/> デリゲート。</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is <c>null</c>.</exception>
        public static MiddlewareFunc<TContext, Task<PredicateFilterFunc<T>>> ToMiddleware<T, TContext>(this IPredicateFilter<T, TContext> filter) {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }

            return new MiddlewareFunc<TContext, Task<PredicateFilterFunc<T>>>(filter.Middleware);
        }
    }

    /// <summary>
    /// <typeparamref name="T"/> の述語フィルター デリゲート。
    /// </summary>
    /// <typeparam name="T">フィルター処理の対象となるエンティティの型。</typeparam>
    /// <param name="entity">フィルター処理の対象となるエンティティ。</param>
    /// <returns><paramref name="entity"/> が述語フィルターの条件を満たせば <c>true</c>、それ以外は <c>false</c>。</returns>
    public delegate bool PredicateFilterFunc<T>(T entity);

    /// <summary>
    /// <see cref="IPredicateFilter{T, TContext}"/> の抽象クラス。
    /// 既定の実装ではフィルター処理を行いません。
    /// </summary>
    public abstract class PredicateFilter<T, TContext> : IPredicateFilter<T, TContext> {

        /// <summary>
        /// 無条件に <c>true</c> を返す述語フィルター デリゲート。
        /// </summary>
        public static readonly PredicateFilterFunc<T> NullFilter = _ => true;

        /// <summary>
        /// <see cref="IPredicateFilter{T, TContext}.Middleware(Func{TContext, Task{PredicateFilterFunc{T}}})"/> の実装。
        /// 既定の実装では <see cref="CreateAsync(TContext, Func{TContext, Task{PredicateFilterFunc{T}}})"/> に処理を委譲します。
        /// </summary>
        public virtual Func<TContext, Task<PredicateFilterFunc<T>>> Middleware(Func<TContext, Task<PredicateFilterFunc<T>>> next) => context => CreateAsync(context, next);

        /// <summary>
        /// <typeparamref name="T"/> の述語フィルター デリゲートを作成します。
        /// 既定の実装では <see cref="Create(TContext, ref bool)"/> に処理を委譲します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>
        /// このフィルター コンポーネント以降の処理を表す述語フィルター デリゲート。常に非 <c>null</c>。
        /// デリゲートのパラメーターも常に非 <c>null</c>。
        /// </returns>
        protected virtual Task<PredicateFilterFunc<T>> CreateAsync(TContext context, Func<TContext, Task<PredicateFilterFunc<T>>> next) {
            Debug.Assert(context != null);
            Debug.Assert(next != null);

            var cancelled = false;
            PredicateFilterFunc<T> filterFunc = Create(context, ref cancelled);
            Debug.Assert(filterFunc != null);
            if (cancelled) { return Task.FromResult(filterFunc); }

            if (filterFunc == NullFilter) { return next(context); }
            return InternalAsync();

            async Task<PredicateFilterFunc<T>> InternalAsync() {
                PredicateFilterFunc<T> nextFunc = await next(context).ConfigureAwait(false);
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
        protected virtual PredicateFilterFunc<T> Create(TContext context, ref bool cancelled) => NullFilter;
    }
}
