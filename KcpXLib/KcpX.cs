using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Interfaces;
using KcpXLib.Utils;

namespace KcpXLib;

/// <summary>
/// キーエンス KV シリーズ PLC の上位リンク通信クラス
/// </summary>
/// <remarks>
/// <see cref="HostLink"/> を拡張して、1 コマンドあたりの点数の上限を超える読み書きなどを行うクラスです。<br/>
/// 各メソッドは、通信が指定したタイムアウト時間内に完了しない場合に <see cref="TimeoutException"/> をスローします。
/// TCP 交信でタイムアウト・通信エラーが発生すると接続を閉じ、以降の呼び出しは <see cref="IOException"/> になります（インスタンスを作り直してください）。
/// </remarks>
public partial class KcpX : HostLink
{
    /// <summary>
    /// 上位リンクの既定のポート番号
    /// </summary>
    public const int DEFAULT_PORT = 8501;

    /// <summary>
    /// インスタンス初期化
    /// </summary>
    /// <remarks>
    /// PLC（EtherNet/IP ユニット）の上位リンク通信の設定に合わせた接続情報を指定します。
    /// </remarks>
    /// <param name="ip">PLC の IP アドレスを指定します。</param>
    /// <param name="port">上位リンクのポート番号を指定します。（デフォルトは、<c>8501</c>です。）</param>
    /// <param name="isUdp">UDP による交信を行う場合に<c>true</c>を指定します。（デフォルトは、TCP 交信:<c>false</c>です。）</param>
    /// <param name="timeoutMilliseconds">通信タイムアウト時間（ミリ秒）を指定します。<c>0</c> は無期限です。（デフォルトは、5 秒です。）</param>
    /// <param name="series">PLC のシリーズを指定します。点数の上限の判定に使用します。（デフォルトは、<c>PlcSeries.KV8000</c>です。）</param>
    /// <exception cref="TimeoutException">指定したタイムアウト時間内に接続できなかった場合にスローします。</exception>
    /// <exception cref="System.Net.Sockets.SocketException">接続が拒否された場合など、接続に失敗した場合にスローします。</exception>
    public KcpX(
        string ip,
        int port = DEFAULT_PORT,
        bool isUdp = false,
        ushort timeoutMilliseconds = 5000,
        PlcSeries series = PlcSeries.KV8000
    ) : base(ip, port, isUdp, timeoutMilliseconds, series)
    {
    }

    internal KcpX(IPlcTransport transport, PlcSeries series = PlcSeries.KV8000) : base(transport, series)
    {
    }

    /// <summary>
    /// 一括読出し
    /// </summary>
    /// <remarks>
    /// 連続したデバイスを読み出します。点数の上限を超える場合は、複数回の RDS コマンドに分割して読み出します。
    /// </remarks>
    /// <typeparam name="T">読み出す値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">読出し対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">読出し対象の先頭アドレスを指定します。</param>
    /// <param name="length">読み出す要素数（<typeparamref name="T"/> の要素数）を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">要素数に <c>0</c> を指定した場合にスローします。</exception>
    /// <exception cref="NotSupportedException">デバイスと型の組み合わせが読出しできない場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    /// <returns>読み出した値を返します。</returns>
    public T[] BatchRead<T>(Prefix prefix, string address, ushort length) where T : unmanaged
    {
        var values = new List<T>(length);
        foreach (var (chunkAddress, chunkLength) in SplitRead<T>(prefix, address, length))
        {
            values.AddRange(ContinuousRead<T>(prefix, chunkAddress, chunkLength));
        }
        return values.ToArray();
    }

    /// <inheritdoc cref="BatchRead{T}(Prefix, string, ushort)"/>
    /// <summary>
    /// 一括読出し（非同期）
    /// </summary>
    public async Task<T[]> BatchReadAsync<T>(Prefix prefix, string address, ushort length) where T : unmanaged
    {
        var values = new List<T>(length);
        foreach (var (chunkAddress, chunkLength) in SplitRead<T>(prefix, address, length))
        {
            values.AddRange(await ContinuousReadAsync<T>(prefix, chunkAddress, chunkLength));
        }
        return values.ToArray();
    }

    /// <summary>
    /// タイマ・カウンタ一括読出し
    /// </summary>
    /// <remarks>
    /// T, C, CTH, CTC の接点・現在値・設定値を読み出します。点数の上限を超える場合は、複数回の RDS コマンドに分割して読み出します。
    /// </remarks>
    /// <param name="prefix">読出し対象のデバイスの接頭辞（T, C, CTH, CTC）を指定します。</param>
    /// <param name="address">読出し対象の先頭アドレスを指定します。</param>
    /// <param name="length">読み出す点数を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">点数に <c>0</c> を指定した場合にスローします。</exception>
    /// <exception cref="NotSupportedException">T, C, CTH, CTC 以外のデバイスを指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    /// <returns>読み出した値を返します。</returns>
    public TimerCounterValue[] BatchReadTimerCounter(Prefix prefix, string address, ushort length)
    {
        var values = new List<TimerCounterValue>(length);
        foreach (var (chunkAddress, chunkLength) in Split(prefix, address, length, DeviceConverter.GetMaxPoints(prefix, DataFormat.D, Series), 1))
        {
            values.AddRange(ContinuousReadTimerCounter(prefix, chunkAddress, chunkLength));
        }
        return values.ToArray();
    }

    /// <inheritdoc cref="BatchReadTimerCounter(Prefix, string, ushort)"/>
    /// <summary>
    /// タイマ・カウンタ一括読出し（非同期）
    /// </summary>
    public async Task<TimerCounterValue[]> BatchReadTimerCounterAsync(Prefix prefix, string address, ushort length)
    {
        var values = new List<TimerCounterValue>(length);
        foreach (var (chunkAddress, chunkLength) in Split(prefix, address, length, DeviceConverter.GetMaxPoints(prefix, DataFormat.D, Series), 1))
        {
            values.AddRange(await ContinuousReadTimerCounterAsync(prefix, chunkAddress, chunkLength));
        }
        return values.ToArray();
    }

    /// <summary>
    /// 統合読出し
    /// </summary>
    /// <remarks>
    /// <see cref="ReadBuilder"/> に追加したデバイスを順に読み出し、各コールバックに値を渡します。
    /// </remarks>
    /// <param name="configure">読み出すデバイスとコールバックを <see cref="ReadBuilder"/> に追加する処理を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="NotSupportedException">デバイスと型の組み合わせが読出しできない場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void Read(Action<ReadBuilder> configure)
    {
        var builder = new ReadBuilder();
        configure(builder);
        foreach (var (read, _) in builder.entries)
        {
            read(this);
        }
    }

    /// <inheritdoc cref="Read(Action{ReadBuilder})"/>
    /// <summary>
    /// 統合読出し（非同期）
    /// </summary>
    public async Task ReadAsync(Action<ReadBuilder> configure)
    {
        var builder = new ReadBuilder();
        configure(builder);
        foreach (var (_, readAsync) in builder.entries)
        {
            await readAsync(this);
        }
    }

    /// <summary>
    /// 一括書込み
    /// </summary>
    /// <remarks>
    /// 連続したデバイスに書き込みます。点数の上限を超える場合は、複数回の WRS コマンドに分割して書き込みます。
    /// </remarks>
    /// <typeparam name="T">書き込む値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">書込み対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">書込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込む値を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">値を 1 つも指定しない場合にスローします。</exception>
    /// <exception cref="NotSupportedException">デバイスと型の組み合わせが書込みできない場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void BatchWrite<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        int offset = 0;
        foreach (var (chunkAddress, chunkLength) in SplitWrite<T>(prefix, address, values.Length))
        {
            ContinuousWrite(prefix, chunkAddress, Slice(values, offset, chunkLength));
            offset += chunkLength;
        }
    }

    /// <inheritdoc cref="BatchWrite{T}(Prefix, string, T[])"/>
    /// <summary>
    /// 一括書込み（非同期）
    /// </summary>
    public async Task BatchWriteAsync<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        int offset = 0;
        foreach (var (chunkAddress, chunkLength) in SplitWrite<T>(prefix, address, values.Length))
        {
            await ContinuousWriteAsync(prefix, chunkAddress, Slice(values, offset, chunkLength));
            offset += chunkLength;
        }
    }

    /// <summary>
    /// 設定値一括書込み
    /// </summary>
    /// <remarks>
    /// T, C, CTC の連続したデバイスの設定値に書き込みます。点数の上限を超える場合は、複数回の WSS コマンドに分割して書き込みます。
    /// </remarks>
    /// <typeparam name="T">書き込む値の型（short, ushort, int, uint）</typeparam>
    /// <param name="prefix">書込み対象のデバイスの接頭辞（T, C, CTC）を指定します。</param>
    /// <param name="address">書込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込む設定値を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">値を 1 つも指定しない場合にスローします。</exception>
    /// <exception cref="NotSupportedException">T, C, CTC 以外のデバイス、または書込みできない型を指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void BatchWriteSetValue<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        WriteValidator.ValidateSetValue(prefix);
        int offset = 0;
        foreach (var (chunkAddress, chunkLength) in SplitWrite<T>(prefix, address, values.Length))
        {
            ContinuousWriteSetValue(prefix, chunkAddress, Slice(values, offset, chunkLength));
            offset += chunkLength;
        }
    }

    /// <inheritdoc cref="BatchWriteSetValue{T}(Prefix, string, T[])"/>
    /// <summary>
    /// 設定値一括書込み（非同期）
    /// </summary>
    public async Task BatchWriteSetValueAsync<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        WriteValidator.ValidateSetValue(prefix);
        int offset = 0;
        foreach (var (chunkAddress, chunkLength) in SplitWrite<T>(prefix, address, values.Length))
        {
            await ContinuousWriteSetValueAsync(prefix, chunkAddress, Slice(values, offset, chunkLength));
            offset += chunkLength;
        }
    }

    /// <summary>
    /// 一括強制セット
    /// </summary>
    /// <remarks>
    /// 指定したアドレスから連続したビットデバイスを ON にします。16 点を超える場合は、複数回の STS コマンドに分割します。
    /// </remarks>
    /// <param name="prefix">対象のデバイスの接頭辞（R, B, MR, LR, CR, VB）を指定します。</param>
    /// <param name="address">対象の先頭アドレスを指定します。</param>
    /// <param name="count">点数を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">点数に <c>0</c> を指定した場合にスローします。</exception>
    /// <exception cref="NotSupportedException">ビットデバイス以外を指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void BatchForceSet(Prefix prefix, string address, ushort count)
    {
        foreach (var (chunkAddress, chunkLength) in SplitForce(prefix, address, count))
        {
            ContinuousForceSet(prefix, chunkAddress, (byte)chunkLength);
        }
    }

    /// <inheritdoc cref="BatchForceSet(Prefix, string, ushort)"/>
    /// <summary>
    /// 一括強制セット（非同期）
    /// </summary>
    public async Task BatchForceSetAsync(Prefix prefix, string address, ushort count)
    {
        foreach (var (chunkAddress, chunkLength) in SplitForce(prefix, address, count))
        {
            await ContinuousForceSetAsync(prefix, chunkAddress, (byte)chunkLength);
        }
    }

    /// <summary>
    /// 一括強制リセット
    /// </summary>
    /// <remarks>
    /// 指定したアドレスから連続したビットデバイスを OFF にします。16 点を超える場合は、複数回の RSS コマンドに分割します。
    /// </remarks>
    /// <param name="prefix">対象のデバイスの接頭辞（R, B, MR, LR, CR, VB）を指定します。</param>
    /// <param name="address">対象の先頭アドレスを指定します。</param>
    /// <param name="count">点数を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">点数に <c>0</c> を指定した場合にスローします。</exception>
    /// <exception cref="NotSupportedException">ビットデバイス以外を指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void BatchForceReset(Prefix prefix, string address, ushort count)
    {
        foreach (var (chunkAddress, chunkLength) in SplitForce(prefix, address, count))
        {
            ContinuousForceReset(prefix, chunkAddress, (byte)chunkLength);
        }
    }

    /// <inheritdoc cref="BatchForceReset(Prefix, string, ushort)"/>
    /// <summary>
    /// 一括強制リセット（非同期）
    /// </summary>
    public async Task BatchForceResetAsync(Prefix prefix, string address, ushort count)
    {
        foreach (var (chunkAddress, chunkLength) in SplitForce(prefix, address, count))
        {
            await ContinuousForceResetAsync(prefix, chunkAddress, (byte)chunkLength);
        }
    }

    /// <summary>
    /// 統合書込み
    /// </summary>
    /// <remarks>
    /// <see cref="WriteBuilder"/> に追加したデバイスへ、追加した順に書き込みます。
    /// </remarks>
    /// <param name="configure">書き込むデバイスと値を <see cref="WriteBuilder"/> に追加する処理を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="NotSupportedException">デバイスと型の組み合わせが書込みできない場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void Write(Action<WriteBuilder> configure)
    {
        var builder = new WriteBuilder();
        configure(builder);
        foreach (var (write, _) in builder.entries)
        {
            write(this);
        }
    }

    /// <inheritdoc cref="Write(Action{WriteBuilder})"/>
    /// <summary>
    /// 統合書込み（非同期）
    /// </summary>
    public async Task WriteAsync(Action<WriteBuilder> configure)
    {
        var builder = new WriteBuilder();
        configure(builder);
        foreach (var (_, writeAsync) in builder.entries)
        {
            await writeAsync(this);
        }
    }

    private IEnumerable<(string Address, ushort Length)> SplitWrite<T>(Prefix prefix, string address, int length) where T : unmanaged
    {
        WriteValidator.Validate<T>(prefix);
        int maxPoints = DeviceConverter.GetMaxPoints(prefix, ValueConverter.GetFormat<T>(), Series);
        return Split(prefix, address, length, maxPoints / ValueConverter.GetPoints<T>(), ValueConverter.GetStride<T>(prefix));
    }

    private static IEnumerable<(string Address, ushort Length)> SplitForce(Prefix prefix, string address, ushort count)
    {
        if (!DeviceConverter.IsBitDevice(prefix))
        {
            throw new NotSupportedException($"Device {prefix} cannot be force set or reset continuously.");
        }
        return Split(prefix, address, count, ContinuousForceSetResetCommand.MAX_COUNT, 1);
    }

    private static T[] Slice<T>(T[] values, int offset, int length)
    {
        var slice = new T[length];
        Array.Copy(values, offset, slice, 0, length);
        return slice;
    }

    private IEnumerable<(string Address, ushort Length)> SplitRead<T>(Prefix prefix, string address, ushort length) where T : unmanaged
    {
        ReadValidator.Validate<T>(prefix);
        int maxPoints = DeviceConverter.GetMaxPoints(prefix, ReadValidator.GetFormat<T>(prefix), Series);
        return Split(prefix, address, length, maxPoints / ValueConverter.GetPoints<T>(), ValueConverter.GetStride<T>(prefix));
    }

    /// <summary>
    /// <paramref name="length"/> 要素を <paramref name="maxLength"/> 要素ずつに分割し、各先頭アドレスと要素数を返します。
    /// </summary>
    internal static IEnumerable<(string Address, ushort Length)> Split(Prefix prefix, string address, int length, int maxLength, int stride)
    {
        if (length < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "The length must be 1 or more.");
        }

        var chunks = new List<(string, ushort)>();
        for (int offset = 0; offset < length; offset += maxLength)
        {
            chunks.Add((
                DeviceConverter.GetOffsetAddress(prefix, address, offset * stride),
                (ushort)Math.Min(maxLength, length - offset)));
        }
        return chunks;
    }
}
