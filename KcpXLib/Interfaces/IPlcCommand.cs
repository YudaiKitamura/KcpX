namespace KcpXLib.Interfaces;

/// <summary>
/// 上位リンクのコマンド
/// </summary>
/// <typeparam name="T">コマンドの実行結果の型</typeparam>
public interface IPlcCommand<T> : IPacketBuilder
{
    /// <summary>
    /// コマンドを実行します。
    /// </summary>
    T Execute(IPlc plc);

    /// <summary>
    /// コマンドを非同期で実行します。
    /// </summary>
    Task<T> ExecuteAsync(IPlc plc);
}
