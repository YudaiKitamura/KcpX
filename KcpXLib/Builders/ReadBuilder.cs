using KcpXLib.Enums;

namespace KcpXLib;

/// <summary>
/// 統合読出しのリクエストビルダー
/// </summary>
/// <remarks>
/// <see cref="KcpX.Read(Action{ReadBuilder})"/> で、追加した順にデバイスを読み出し、各コールバックへ値を渡します。
/// </remarks>
public sealed class ReadBuilder
{
    internal readonly List<(Action<KcpX> read, Func<KcpX, Task> readAsync)> entries = new();

    /// <summary>
    /// 読み出す単一デバイスと、読出し後に値を受け取るコールバックを追加します。
    /// </summary>
    /// <typeparam name="T">読み出す値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">読出し対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">読出し対象のアドレスを指定します。</param>
    /// <param name="onRead">読み出した値を受け取るコールバックを指定します。</param>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public ReadBuilder Add<T>(Prefix prefix, string address, Action<T> onRead) where T : unmanaged
    {
        entries.Add((
            k => onRead(k.Read<T>(prefix, address)),
            async k => onRead(await k.ReadAsync<T>(prefix, address))
        ));
        return this;
    }

    /// <summary>
    /// 読み出す連続デバイスと、読出し後に値を受け取るコールバックを追加します。
    /// </summary>
    /// <typeparam name="T">読み出す値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">読出し対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">読出し対象の先頭アドレスを指定します。</param>
    /// <param name="length">読み出す要素数（<typeparamref name="T"/> の要素数）を指定します。</param>
    /// <param name="onRead">読み出した値を受け取るコールバックを指定します。</param>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public ReadBuilder Add<T>(Prefix prefix, string address, ushort length, Action<T[]> onRead) where T : unmanaged
    {
        entries.Add((
            k => onRead(k.BatchRead<T>(prefix, address, length)),
            async k => onRead(await k.BatchReadAsync<T>(prefix, address, length))
        ));
        return this;
    }
}
