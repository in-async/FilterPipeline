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
        private readonly Func<TContext, Task<Func<T, bool>>> _predicateCreator;

        /// <summary>
        /// <see cref="PredicateSequenceFilter{T, TContext}"/> クラスの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="predicateCreator">処理の中核となる、述語フィルターを構成するデリゲート。</param>
        /// <exception cref="ArgumentNullException"><paramref name="predicateCreator"/> is <c>null</c>.</exception>
        public PredicateSequenceFilter(Func<TContext, Task<Func<T, bool>>> predicateCreator) {
            _predicateCreator = predicateCreator ?? throw new ArgumentNullException(nameof(predicateCreator));
        }

        /// <summary>
        /// <see cref="ISequenceFilter{T, TContext}.CreateAsync(TContext, Func{Task{SequenceFilterFunc{T}}})"/> の実装。
        /// </summary>
        public async Task<SequenceFilterFunc<T>> CreateAsync(TContext context, Func<Task<SequenceFilterFunc<T>>> next) {
            Debug.Assert(context != null);
            Debug.Assert(next != null);

            Func<T, bool> predicate = await _predicateCreator(context).ConfigureAwait(false);
            Debug.Assert(predicate != null);

            SequenceFilterFunc<T> nextFunc = await next().ConfigureAwait(false);
            Debug.Assert(nextFunc != null);

            if (predicate == PredicateFilter<T, TContext>.NullFilter) { return nextFunc; }
            return source => nextFunc(source.Where(predicate));
        }
    }
}
