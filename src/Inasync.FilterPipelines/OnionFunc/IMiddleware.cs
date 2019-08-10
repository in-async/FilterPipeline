using System;

namespace Inasync.OnionFunc {

    /// <summary>
    /// オニオン パイプラインを構成するミドルウェア コンポーネント インターフェース。
    /// </summary>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    /// <typeparam name="TResult">パイプラインの実行結果の型。</typeparam>
    public interface IMiddleware<TContext, TResult> {

        /// <summary>
        /// ミドルウェアで定義されている処理を実行します。
        /// </summary>
        /// <param name="context">パイプラインの実行時コンテキスト。</param>
        /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
        /// <returns>このコンポーネント以降の処理を表すタスク。常に非 <c>null</c>。</returns>
        TResult Invoke(TContext context, Func<TContext, TResult> next);
    }

    /// <summary>
    /// ミドルウェアで定義されている処理を実行します。
    /// </summary>
    /// <typeparam name="TContext">パイプラインの実行時コンテキストの型。</typeparam>
    /// <typeparam name="TResult">パイプラインの実行結果の型。</typeparam>
    /// <param name="context">パイプラインの実行時コンテキスト。</param>
    /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
    /// <returns>このミドルウェア コンポーネント以降の処理を表すタスク。常に非 <c>null</c>。</returns>
    public delegate TResult MiddlewareFunc<TContext, TResult>(TContext context, Func<TContext, TResult> next);
}
