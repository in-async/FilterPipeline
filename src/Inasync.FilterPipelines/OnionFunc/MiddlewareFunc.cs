using System;

namespace Inasync {

    /// <summary>
    /// ミドルウェアで定義されている処理を組み込んだ新しいパイプライン関数を作成します。
    /// </summary>
    /// <typeparam name="T">パイプラインの実行時パラメーターの型。</typeparam>
    /// <typeparam name="TResult">パイプラインの実行結果の型。</typeparam>
    /// <param name="next">パイプラインの後続のコンポーネントを表すデリゲート。常に非 <c>null</c>。呼ばない事により残りのコンポーネントをショートサーキットできます。</param>
    /// <returns>このミドルウェアを組み込んだ新しいパイプライン関数。常に非 <c>null</c>。</returns>
    public delegate Func<T, TResult> MiddlewareFunc<T, TResult>(Func<T, TResult> next);
}
