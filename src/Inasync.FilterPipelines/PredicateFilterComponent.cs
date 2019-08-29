using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// 述語処理をフィルター処理に変換するクラス。
    /// </summary>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    /// <typeparam name="TEntity">フィルター処理の対象となる要素の型。</typeparam>
    public sealed class PredicateFilterComponent<TContext, TEntity> : IFilterComponent<TContext, TEntity> {
        private readonly Func<TContext, Task<PredicateFunc<TEntity>>> _predicateCreator;

        /// <summary>
        /// <see cref="PredicateFilterComponent{TContext, TEntity}"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="predicateCreator">処理の中核となる、述語フィルターを構成するデリゲート。</param>
        /// <exception cref="ArgumentNullException"><paramref name="predicateCreator"/> is <c>null</c>.</exception>
        public PredicateFilterComponent(Func<TContext, Task<PredicateFunc<TEntity>>> predicateCreator) {
            _predicateCreator = predicateCreator ?? throw new ArgumentNullException(nameof(predicateCreator));
        }

        /// <summary>
        /// <see cref="IFilterComponent{TContext, TEntity}.Middleware(Func{TContext, Task{FilterFunc{TEntity}}})"/> の実装。
        /// </summary>
        public Func<TContext, Task<FilterFunc<TEntity>>> Middleware(Func<TContext, Task<FilterFunc<TEntity>>> next) {
            Debug.Assert(next != null);

            return async context => {
                Debug.Assert(context != null);

                PredicateFunc<TEntity> predicateFunc = await _predicateCreator(context).ConfigureAwait(false);
                Debug.Assert(predicateFunc != null);

                FilterFunc<TEntity> nextFilter = await next(context).ConfigureAwait(false);
                Debug.Assert(nextFilter != null);

                if (predicateFunc == PredicateComponent<TContext, TEntity>.NullPredicate) { return nextFilter; }

                var predicate = new Func<TEntity, bool>(predicateFunc);
                return source => nextFilter(source.Where(predicate));
            };
        }
    }
}
