using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// 述語フィルターを構成するデリゲートをシーケンス フィルターに変換するクラス。
    /// </summary>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    /// <typeparam name="TEntity">フィルター処理の対象となる要素の型。</typeparam>
    public sealed class PredicateSequenceFilter<TContext, TEntity> : ISequenceFilter<TContext, TEntity> {
        private readonly Func<TContext, Task<PredicateFilterFunc<TEntity>>> _predicateCreator;

        /// <summary>
        /// <see cref="PredicateSequenceFilter{TContext, TEntity}"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="predicateCreator">処理の中核となる、述語フィルターを構成するデリゲート。</param>
        /// <exception cref="ArgumentNullException"><paramref name="predicateCreator"/> is <c>null</c>.</exception>
        public PredicateSequenceFilter(Func<TContext, Task<PredicateFilterFunc<TEntity>>> predicateCreator) {
            _predicateCreator = predicateCreator ?? throw new ArgumentNullException(nameof(predicateCreator));
        }

        /// <summary>
        /// <see cref="ISequenceFilter{TContext, TEntity}.Middleware(Func{TContext, Task{SequenceFilterFunc{TEntity}}})"/> の実装。
        /// </summary>
        public Func<TContext, Task<SequenceFilterFunc<TEntity>>> Middleware(Func<TContext, Task<SequenceFilterFunc<TEntity>>> next) {
            Debug.Assert(next != null);

            return async context => {
                Debug.Assert(context != null);

                PredicateFilterFunc<TEntity> predicateFunc = await _predicateCreator(context).ConfigureAwait(false);
                Debug.Assert(predicateFunc != null);

                SequenceFilterFunc<TEntity> nextFunc = await next(context).ConfigureAwait(false);
                Debug.Assert(nextFunc != null);

                if (predicateFunc == PredicateFilter<TContext, TEntity>.NullFilter) { return nextFunc; }

                var predicate = new Func<TEntity, bool>(predicateFunc);
                return source => nextFunc(source.Where(predicate));
            };
        }
    }
}
