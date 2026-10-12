using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Utils;

namespace KcpXLib;

/// <summary>
/// モニタ登録のリクエストビルダー
/// </summary>
/// <remarks>
/// <see cref="Add{T}(Prefix, string, Action{T})"/> でモニタ対象のデバイスと、読出し後に呼ばれるコールバックを追加します。
/// <c>bool</c> はビットモニタ（MBS / MBR）、それ以外の型はワードモニタ（MWS / MWR）に振り分けられます。
/// ビット・ワードそれぞれ最大 120 点まで登録できます（<c>long</c> / <c>ulong</c> / <c>double</c> は 1 つで 2 点を使用します）。
/// </remarks>
public sealed class MonitorBuilder
{
    internal readonly List<(Prefix prefix, string address, Action<bool> apply)> bitEntries = new();
    internal readonly List<(Prefix prefix, string address, DataFormat format)> wordPoints = new();
    internal readonly List<(int points, Action<string[]> apply)> wordEntries = new();

    /// <summary>
    /// モニタ対象のデバイスと、モニタ読出し後に値を受け取るコールバックを追加します。
    /// </summary>
    /// <remarks>
    /// リレー（R, MR, LR, CR）をワード系の型でモニタする場合は、チャンネル単位（ビット番号 00）で指定してください。<br/>
    /// T, C の現在値・設定値は TC/TS, CC/CS を指定してください。CTH, CTC のワードモニタは KV-5500 / KV Nano シリーズのみ対応しています。
    /// </remarks>
    /// <typeparam name="T">読み出す値の型（bool, ushort, short, uint, int, float, ulong, long, double）</typeparam>
    /// <param name="prefix">モニタ対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">モニタ対象のアドレスを指定します。</param>
    /// <param name="onRead">モニタ読出し後に値を受け取るコールバックを指定します。</param>
    /// <exception cref="Exceptions.DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="NotSupportedException">モニタできないデバイスと型の組み合わせを指定した場合にスローします。</exception>
    /// <returns>メソッドチェーン用に自身を返します。</returns>
    public MonitorBuilder Add<T>(Prefix prefix, string address, Action<T> onRead) where T : unmanaged
    {
        DeviceConverter.ToIndex(prefix, address);
        if (typeof(T) == typeof(bool))
        {
            MonitorBitRegistCommand.Validate(prefix);
            bitEntries.Add((prefix, address, v => onRead((T)(object)v)));
            return this;
        }

        ValueConverter.Validate<T>(prefix);
        MonitorWordRegistCommand.Validate(prefix, address);

        var format = ValueConverter.GetFormat<T>();
        int points = ValueConverter.GetPoints<T>();
        for (int i = 0; i < points; i++)
        {
            wordPoints.Add((prefix, DeviceConverter.GetOffsetAddress(prefix, address, i * ValueConverter.GetStride<T>(prefix) / points), format));
        }
        wordEntries.Add((points, tokens => onRead(ValueConverter.FromTokens<T>(tokens)[0])));
        return this;
    }
}
