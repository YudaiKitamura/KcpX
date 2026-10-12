using KcpXLib.Enums;

namespace KcpXLib;

/// <summary>
/// 統合書込みのリクエストビルダー
/// </summary>
/// <remarks>
/// <see cref="KcpX.Write(Action{WriteBuilder})"/> で、追加した順にデバイスへ書き込みます。
/// </remarks>
public sealed class WriteBuilder
{
    internal readonly List<(Action<KcpX> write, Func<KcpX, Task> writeAsync)> entries = new();

    /// <summary>
    /// 書き込む単一デバイスと値を追加します。
    /// </summary>
    /// <typeparam name="T">書き込む値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">書込み対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">書込み対象のアドレスを指定します。</param>
    /// <param name="value">書き込む値を指定します。</param>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public WriteBuilder Add<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        entries.Add((
            k => k.Write(prefix, address, value),
            k => k.WriteAsync(prefix, address, value)
        ));
        return this;
    }

    /// <summary>
    /// 書き込む連続デバイスと値を追加します。
    /// </summary>
    /// <typeparam name="T">書き込む値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">書込み対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">書込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込む値を指定します。</param>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public WriteBuilder Add<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        entries.Add((
            k => k.BatchWrite(prefix, address, values),
            k => k.BatchWriteAsync(prefix, address, values)
        ));
        return this;
    }
}
