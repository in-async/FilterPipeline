using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// 任意の述語によってフィルター処理を行うミドルウェア クラス。
    /// </summary>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    /// <typeparam name="TEntity">フィルター処理の対象となる要素の型。</typeparam>
    public sealed class PredicateFilterMiddleware<TContext, TEntity> : IFilterMiddleware<TContext, TEntity> {
        private readonly Func<TContext, Task<PredicateFunc<TEntity>>> _predicateCreator;

        /// <summary>
        /// <see cref="PredicateFilterMiddleware{TContext, TEntity}"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="predicateCreator">処理の中核となる、述語を構成するデリゲート。</param>
        /// <exception cref="ArgumentNullException"><paramref name="predicateCreator"/> is <c>null</c>.</exception>
        public PredicateFilterMiddleware(Func<TContext, Task<PredicateFunc<TEntity>>> predicateCreator) {
            _predicateCreator = predicateCreator ?? throw new ArgumentNullException(nameof(predicateCreator));
        }

        /// <summary>
        /// <see cref="IMiddleware{T, TResult}.Invoke(Func{T, TResult})"/> の実装。
        /// </summary>
        public Func<TContext, Task<FilterFunc<TEntity>>> Invoke(Func<TContext, Task<FilterFunc<TEntity>>> next) {
            Debug.Assert(next != null);

            return async context => {
                Debug.Assert(context != null);

                PredicateFunc<TEntity> predicate = await _predicateCreator(context).ConfigureAwait(false);
                Debug.Assert(predicate != null);

                FilterFunc<TEntity> nextFilter = await next(context).ConfigureAwait(false);
                Debug.Assert(nextFilter != null);

                if (predicate == PredicateMiddleware<TContext, TEntity>.NullPredicate) { return nextFilter; }

                var predicateFunc = new Func<TEntity, bool>(predicate);
                return source => nextFilter(source.Where(predicateFunc));
            };
        }
    }
}
