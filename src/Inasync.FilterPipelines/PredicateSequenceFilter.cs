using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// 述語フィルターを構成するデリゲートをシーケンス フィルターに変換するクラス。
    /// </summary>
    /// <typeparam name="T">フィルター処理の対象となる要素の型。</typeparam>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    public sealed class PredicateSequenceFilter<T, TContext> : ISequenceFilter<T, TContext> {
        private readonly Func<TContext, Task<PredicateFilterFunc<T>>> _predicateCreator;

        /// <summary>
        /// <see cref="PredicateSequenceFilter{T, TContext}"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="predicateCreator">処理の中核となる、述語フィルターを構成するデリゲート。</param>
        /// <exception cref="ArgumentNullException"><paramref name="predicateCreator"/> is <c>null</c>.</exception>
        public PredicateSequenceFilter(Func<TContext, Task<PredicateFilterFunc<T>>> predicateCreator) {
            _predicateCreator = predicateCreator ?? throw new ArgumentNullException(nameof(predicateCreator));
        }

        /// <summary>
        /// <see cref="ISequenceFilter{T, TContext}.Middleware(Func{TContext, Task{SequenceFilterFunc{T}}})"/> の実装。
        /// </summary>
        public Func<TContext, Task<SequenceFilterFunc<T>>> Middleware(Func<TContext, Task<SequenceFilterFunc<T>>> next) {
            Debug.Assert(next != null);

            return async context => {
                Debug.Assert(context != null);

                PredicateFilterFunc<T> predicateFunc = await _predicateCreator(context).ConfigureAwait(false);
                Debug.Assert(predicateFunc != null);

                SequenceFilterFunc<T> nextFunc = await next(context).ConfigureAwait(false);
                Debug.Assert(nextFunc != null);

                if (predicateFunc == PredicateFilter<T, TContext>.NullFilter) { return nextFunc; }

                var predicate = new Func<T, bool>(predicateFunc);
                return source => nextFunc(source.Where(predicate));
            };
        }
    }
}
