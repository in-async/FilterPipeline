using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Inasync.FilterPipelines {

    /// <summary>
    /// パイプラインを構成する述語ミドルウェアのインターフェース。
    /// </summary>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    /// <typeparam name="TEntity">述語処理の対象となるエンティティの型。</typeparam>
    public interface IPredicateMiddleware<TContext, TEntity> : IMiddleware<TContext, Task<PredicateFunc<TEntity>>> { }

    /// <summary>
    /// <typeparamref name="TEntity"/> の述語デリゲート。
    /// </summary>
    /// <typeparam name="TEntity">述語処理の対象となるエンティティの型。</typeparam>
    /// <param name="entity">述語処理の対象となるエンティティ。</param>
    /// <returns><paramref name="entity"/> が述語の条件を満たせば <c>true</c>、それ以外は <c>false</c>。</returns>
    public delegate bool PredicateFunc<TEntity>(TEntity entity);

    /// <summary>
    /// <see cref="IPredicateMiddleware{TContext, TEntity}"/> の抽象クラス。
    /// 既定の実装では述語は <see cref="NullPredicate"/> です。
    /// </summary>
    public abstract class PredicateMiddleware<TContext, TEntity> : IPredicateMiddleware<TContext, TEntity> {

        /// <summary>
        /// 無条件に <c>true</c> を返す述語デリゲート。
        /// </summary>
        public static readonly PredicateFunc<TEntity> NullPredicate = _ => true;

        /// <summary>
        /// <see cref="IMiddleware{T, TResult}.Invoke(Func{T, TResult})"/> の実装。
        /// 既定の実装では <see cref="CreateAsync(TContext, Func{TContext, Task{PredicateFunc{TEntity}}})"/> に処理を委譲します。
        /// </summary>
        public virtual Func<TContext, Task<PredicateFunc<TEntity>>> Invoke(Func<TContext, Task<PredicateFunc<TEntity>>> next) => context => CreateAsync(context, next);

        /// <summary>
        /// <typeparamref name="TEntity"/> の述語デリゲートを作成します。
        /// 既定の実装では <see cref="Create(TContext, ref bool)"/> に処理を委譲します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>このコンポーネント以降の述語処理を表すデリゲート。常に非 <c>null</c>。</returns>
        protected virtual Task<PredicateFunc<TEntity>> CreateAsync(TContext context, Func<TContext, Task<PredicateFunc<TEntity>>> next) {
            Debug.Assert(context != null);
            Debug.Assert(next != null);

            var cancelled = false;
            PredicateFunc<TEntity> predicate = Create(context, ref cancelled);
            Debug.Assert(predicate != null);
            if (cancelled) { return Task.FromResult(predicate); }

            if (predicate == NullPredicate) { return next(context); }
            return InternalAsync();

            async Task<PredicateFunc<TEntity>> InternalAsync() {
                PredicateFunc<TEntity> nextPredicate = await next(context).ConfigureAwait(false);
                Debug.Assert(nextPredicate != null);
                if (nextPredicate == NullPredicate) { return predicate; }

                return x => predicate(x) && nextPredicate(x);
            }
        }

        /// <summary>
        /// <see cref="PredicateFunc{TEntity}"/> を作成します。
        /// 既定の実装では <see cref="NullPredicate"/> を返します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。常に非 <c>null</c>。</param>
        /// <param name="cancelled">パイプラインの残りのコンポーネントをショートサーキットする場合は <c>true</c>、それ以外は <c>false</c>。既定値は <c>false</c>。</param>
        /// <returns>このコンポーネント以降の述語処理を表すデリゲート。常に非 <c>null</c>。</returns>
        protected virtual PredicateFunc<TEntity> Create(TContext context, ref bool cancelled) => NullPredicate;
    }
}
