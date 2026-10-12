using KcpXLib.Abstracts;
using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Exceptions;
using KcpXLib.Interfaces;
using KcpXLib.Transports;
using KcpXLib.Utils;

namespace KcpXLib;

/// <summary>
/// 上位リンク通信クラス
/// </summary>
/// <remarks>
/// 上位リンクのコマンドを 1 回ずつ発行するクラスです。点数の上限を超える読み書きは <see cref="KcpX"/> を使用してください。
/// </remarks>
public partial class HostLink : BasePlc, IPlc
{
    /// <summary>
    /// 接続先 PLC のシリーズ
    /// </summary>
    public PlcSeries Series { get; }

    internal HostLink(string ip, int port, bool isUdp, ushort timeout, PlcSeries series)
        : base(isUdp ? new UdpPlcTransport(ip, port, timeout) : new TcpPlcTransport(ip, port, timeout))
    {
        Series = series;
    }

    internal HostLink(IPlcTransport transport, PlcSeries series) : base(transport)
    {
        Series = series;
    }

    /// <summary>
    /// 単一デバイス読出し（コマンド: RD）
    /// </summary>
    /// <remarks>
    /// 型 <typeparamref name="T"/> に応じたデータ形式（bool: ビット、ushort: .U、short: .S、uint/float: .D、int: .L）で読み出します。
    /// 64 ビットの型（long, ulong, double）は .D の 2 点を RDS で読み出します。<br/>
    /// T, C, CTH, CTC は <c>bool</c> で接点の状態を読み出せます。
    /// </remarks>
    /// <typeparam name="T">読み出す値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">読出し対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">読出し対象のアドレスを指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="NotSupportedException">デバイスと型の組み合わせが読出しできない場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    /// <returns>読み出した値を返します。</returns>
    public T Read<T>(Prefix prefix, string address) where T : unmanaged
    {
        if (ValueConverter.GetPoints<T>() != 1)
        {
            return ContinuousRead<T>(prefix, address, 1)[0];
        }
        return new PlcCommandHandler<T>().Execute(new ReadCommand<T>(prefix, address), this);
    }

    /// <inheritdoc cref="Read{T}(Prefix, string)"/>
    /// <summary>
    /// 単一デバイス読出し（非同期、コマンド: RD）
    /// </summary>
    public async Task<T> ReadAsync<T>(Prefix prefix, string address) where T : unmanaged
    {
        if (ValueConverter.GetPoints<T>() != 1)
        {
            return (await ContinuousReadAsync<T>(prefix, address, 1))[0];
        }
        return await new PlcCommandHandler<T>().ExecuteAsync(new ReadCommand<T>(prefix, address), this);
    }

    /// <summary>
    /// 連続データ読出し（コマンド: RDS）
    /// </summary>
    /// <remarks>
    /// 1 回のコマンドで連続したデバイスを読み出します。点数の上限（例: KV-8000 の DM は .U で 1000 点、.D で 500 点）を超える場合は
    /// <see cref="KcpX.BatchRead{T}(Prefix, string, ushort)"/> を使用してください。<br/>
    /// リレーを <c>bool</c> 以外の型で読み出す場合は、指定したアドレスから連続した 16/32 ビットを 1 要素として扱います。
    /// </remarks>
    /// <typeparam name="T">読み出す値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">読出し対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">読出し対象の先頭アドレスを指定します。</param>
    /// <param name="length">読み出す要素数（<typeparamref name="T"/> の要素数）を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">要素数が点数の上限を超える場合にスローします。</exception>
    /// <exception cref="NotSupportedException">デバイスと型の組み合わせが読出しできない場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    /// <returns>読み出した値を返します。</returns>
    public T[] ContinuousRead<T>(Prefix prefix, string address, ushort length) where T : unmanaged
    {
        return new PlcCommandHandler<T[]>().Execute(new ContinuousReadCommand<T>(prefix, address, length, Series), this);
    }

    /// <inheritdoc cref="ContinuousRead{T}(Prefix, string, ushort)"/>
    /// <summary>
    /// 連続データ読出し（非同期、コマンド: RDS）
    /// </summary>
    public async Task<T[]> ContinuousReadAsync<T>(Prefix prefix, string address, ushort length) where T : unmanaged
    {
        return await new PlcCommandHandler<T[]>().ExecuteAsync(new ContinuousReadCommand<T>(prefix, address, length, Series), this);
    }

    /// <summary>
    /// タイマ・カウンタ読出し（コマンド: RDS）
    /// </summary>
    /// <remarks>
    /// T, C, CTH, CTC の接点・現在値・設定値を読み出します。
    /// </remarks>
    /// <param name="prefix">読出し対象のデバイスの接頭辞（T, C, CTH, CTC）を指定します。</param>
    /// <param name="address">読出し対象のアドレスを指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="NotSupportedException">T, C, CTH, CTC 以外のデバイスを指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    /// <returns>読み出した値を返します。</returns>
    public TimerCounterValue ReadTimerCounter(Prefix prefix, string address)
    {
        return ContinuousReadTimerCounter(prefix, address, 1)[0];
    }

    /// <inheritdoc cref="ReadTimerCounter(Prefix, string)"/>
    /// <summary>
    /// タイマ・カウンタ読出し（非同期、コマンド: RDS）
    /// </summary>
    public async Task<TimerCounterValue> ReadTimerCounterAsync(Prefix prefix, string address)
    {
        return (await ContinuousReadTimerCounterAsync(prefix, address, 1))[0];
    }

    /// <summary>
    /// タイマ・カウンタ連続読出し（コマンド: RDS）
    /// </summary>
    /// <remarks>
    /// T, C, CTH, CTC の接点・現在値・設定値を連続して読み出します。
    /// </remarks>
    /// <param name="prefix">読出し対象のデバイスの接頭辞（T, C, CTH, CTC）を指定します。</param>
    /// <param name="address">読出し対象の先頭アドレスを指定します。</param>
    /// <param name="length">読み出す点数を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">点数が上限を超える場合にスローします。</exception>
    /// <exception cref="NotSupportedException">T, C, CTH, CTC 以外のデバイスを指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    /// <returns>読み出した値を返します。</returns>
    public TimerCounterValue[] ContinuousReadTimerCounter(Prefix prefix, string address, ushort length)
    {
        return new PlcCommandHandler<TimerCounterValue[]>().Execute(new ContinuousReadTimerCounterCommand(prefix, address, length, Series), this);
    }

    /// <inheritdoc cref="ContinuousReadTimerCounter(Prefix, string, ushort)"/>
    /// <summary>
    /// タイマ・カウンタ連続読出し（非同期、コマンド: RDS）
    /// </summary>
    public async Task<TimerCounterValue[]> ContinuousReadTimerCounterAsync(Prefix prefix, string address, ushort length)
    {
        return await new PlcCommandHandler<TimerCounterValue[]>().ExecuteAsync(new ContinuousReadTimerCounterCommand(prefix, address, length, Series), this);
    }

    /// <summary>
    /// 単一デバイス書込み（コマンド: WR）
    /// </summary>
    /// <remarks>
    /// 型 <typeparamref name="T"/> に応じたデータ形式（bool: ビット、ushort: .U、short: .S、uint/float: .D、int: .L）で書き込みます。
    /// 64 ビットの型（long, ulong, double）は .D の 2 点を WRS で書き込みます。<br/>
    /// T, C, CTH は現在値、CTC は設定値に書き込みます。32 ビット値の上位・下位の同時性は、デバイス番号が偶数の場合のみ保証されます。
    /// </remarks>
    /// <typeparam name="T">書き込む値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">書込み対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">書込み対象のアドレスを指定します。</param>
    /// <param name="value">書き込む値を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="NotSupportedException">デバイスと型の組み合わせが書込みできない場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void Write<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        if (ValueConverter.GetPoints<T>() != 1)
        {
            ContinuousWrite(prefix, address, [value]);
            return;
        }
        new PlcCommandHandler<bool>().Execute(new WriteCommand<T>(prefix, address, value), this);
    }

    /// <inheritdoc cref="Write{T}(Prefix, string, T)"/>
    /// <summary>
    /// 単一デバイス書込み（非同期、コマンド: WR）
    /// </summary>
    public async Task WriteAsync<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        if (ValueConverter.GetPoints<T>() != 1)
        {
            await ContinuousWriteAsync(prefix, address, [value]);
            return;
        }
        await new PlcCommandHandler<bool>().ExecuteAsync(new WriteCommand<T>(prefix, address, value), this);
    }

    /// <summary>
    /// 連続データ書込み（コマンド: WRS）
    /// </summary>
    /// <remarks>
    /// 1 回のコマンドで連続したデバイスに書き込みます。点数の上限を超える場合は
    /// <see cref="KcpX.BatchWrite{T}(Prefix, string, T[])"/> を使用してください。
    /// </remarks>
    /// <typeparam name="T">書き込む値の型（bool, short, ushort, int, uint, float, long, ulong, double）</typeparam>
    /// <param name="prefix">書込み対象のデバイスの接頭辞を指定します。</param>
    /// <param name="address">書込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込む値を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">値の数が点数の上限を超える場合にスローします。</exception>
    /// <exception cref="NotSupportedException">デバイスと型の組み合わせが書込みできない場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void ContinuousWrite<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        new PlcCommandHandler<bool>().Execute(new ContinuousWriteCommand<T>(prefix, address, values, Series), this);
    }

    /// <inheritdoc cref="ContinuousWrite{T}(Prefix, string, T[])"/>
    /// <summary>
    /// 連続データ書込み（非同期、コマンド: WRS）
    /// </summary>
    public async Task ContinuousWriteAsync<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new ContinuousWriteCommand<T>(prefix, address, values, Series), this);
    }

    /// <summary>
    /// 設定値書込み（コマンド: WS）
    /// </summary>
    /// <remarks>
    /// T, C, CTC の設定値に書き込みます。
    /// </remarks>
    /// <typeparam name="T">書き込む値の型（short, ushort, int, uint）</typeparam>
    /// <param name="prefix">書込み対象のデバイスの接頭辞（T, C, CTC）を指定します。</param>
    /// <param name="address">書込み対象のアドレスを指定します。</param>
    /// <param name="value">書き込む設定値を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="NotSupportedException">T, C, CTC 以外のデバイス、または書込みできない型を指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合（書込みプロテクト中は E4）にスローします。</exception>
    public void WriteSetValue<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        new PlcCommandHandler<bool>().Execute(new WriteCommand<T>(prefix, address, value, isSetValue: true), this);
    }

    /// <inheritdoc cref="WriteSetValue{T}(Prefix, string, T)"/>
    /// <summary>
    /// 設定値書込み（非同期、コマンド: WS）
    /// </summary>
    public async Task WriteSetValueAsync<T>(Prefix prefix, string address, T value) where T : unmanaged
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new WriteCommand<T>(prefix, address, value, isSetValue: true), this);
    }

    /// <summary>
    /// 連続設定値書込み（コマンド: WSS）
    /// </summary>
    /// <remarks>
    /// T, C, CTC の連続したデバイスの設定値に書き込みます。点数の上限を超える場合は
    /// <see cref="KcpX.BatchWriteSetValue{T}(Prefix, string, T[])"/> を使用してください。
    /// </remarks>
    /// <typeparam name="T">書き込む値の型（short, ushort, int, uint）</typeparam>
    /// <param name="prefix">書込み対象のデバイスの接頭辞（T, C, CTC）を指定します。</param>
    /// <param name="address">書込み対象の先頭アドレスを指定します。</param>
    /// <param name="values">書き込む設定値を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">値の数が点数の上限を超える場合にスローします。</exception>
    /// <exception cref="NotSupportedException">T, C, CTC 以外のデバイス、または書込みできない型を指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合（書込みプロテクト中は E4）にスローします。</exception>
    public void ContinuousWriteSetValue<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        new PlcCommandHandler<bool>().Execute(new ContinuousWriteCommand<T>(prefix, address, values, Series, isSetValue: true), this);
    }

    /// <inheritdoc cref="ContinuousWriteSetValue{T}(Prefix, string, T[])"/>
    /// <summary>
    /// 連続設定値書込み（非同期、コマンド: WSS）
    /// </summary>
    public async Task ContinuousWriteSetValueAsync<T>(Prefix prefix, string address, T[] values) where T : unmanaged
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new ContinuousWriteCommand<T>(prefix, address, values, Series, isSetValue: true), this);
    }

    /// <summary>
    /// 強制セット（コマンド: ST）
    /// </summary>
    /// <remarks>
    /// 指定したビットデバイス、またはタイマ・カウンタの接点を ON にします。
    /// </remarks>
    /// <param name="prefix">対象のデバイスの接頭辞（R, B, MR, LR, CR, VB, T, C）を指定します。</param>
    /// <param name="address">対象のアドレスを指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="NotSupportedException">強制セットできないデバイスを指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void ForceSet(Prefix prefix, string address)
    {
        new PlcCommandHandler<bool>().Execute(new ForceSetResetCommand(prefix, address, isSet: true), this);
    }

    /// <inheritdoc cref="ForceSet(Prefix, string)"/>
    /// <summary>
    /// 強制セット（非同期、コマンド: ST）
    /// </summary>
    public async Task ForceSetAsync(Prefix prefix, string address)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new ForceSetResetCommand(prefix, address, isSet: true), this);
    }

    /// <summary>
    /// 強制リセット（コマンド: RS）
    /// </summary>
    /// <remarks>
    /// 指定したビットデバイス、またはタイマ・カウンタの接点を OFF にします。
    /// </remarks>
    /// <param name="prefix">対象のデバイスの接頭辞（R, B, MR, LR, CR, VB, T, C, CTC）を指定します。</param>
    /// <param name="address">対象のアドレスを指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="NotSupportedException">強制リセットできないデバイスを指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void ForceReset(Prefix prefix, string address)
    {
        new PlcCommandHandler<bool>().Execute(new ForceSetResetCommand(prefix, address, isSet: false), this);
    }

    /// <inheritdoc cref="ForceReset(Prefix, string)"/>
    /// <summary>
    /// 強制リセット（非同期、コマンド: RS）
    /// </summary>
    public async Task ForceResetAsync(Prefix prefix, string address)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new ForceSetResetCommand(prefix, address, isSet: false), this);
    }

    /// <summary>
    /// 連続強制セット（コマンド: STS）
    /// </summary>
    /// <remarks>
    /// 指定したアドレスから連続したビットデバイスを ON にします。16 点を超える場合は
    /// <see cref="KcpX.BatchForceSet(Prefix, string, ushort)"/> を使用してください。
    /// </remarks>
    /// <param name="prefix">対象のデバイスの接頭辞（R, B, MR, LR, CR, VB）を指定します。</param>
    /// <param name="address">対象の先頭アドレスを指定します。</param>
    /// <param name="count">点数（1〜16）を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">点数が 1〜16 の範囲外の場合にスローします。</exception>
    /// <exception cref="NotSupportedException">ビットデバイス以外を指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void ContinuousForceSet(Prefix prefix, string address, byte count)
    {
        new PlcCommandHandler<bool>().Execute(new ContinuousForceSetResetCommand(prefix, address, count, isSet: true), this);
    }

    /// <inheritdoc cref="ContinuousForceSet(Prefix, string, byte)"/>
    /// <summary>
    /// 連続強制セット（非同期、コマンド: STS）
    /// </summary>
    public async Task ContinuousForceSetAsync(Prefix prefix, string address, byte count)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new ContinuousForceSetResetCommand(prefix, address, count, isSet: true), this);
    }

    /// <summary>
    /// 連続強制リセット（コマンド: RSS）
    /// </summary>
    /// <remarks>
    /// 指定したアドレスから連続したビットデバイスを OFF にします。16 点を超える場合は
    /// <see cref="KcpX.BatchForceReset(Prefix, string, ushort)"/> を使用してください。
    /// </remarks>
    /// <param name="prefix">対象のデバイスの接頭辞（R, B, MR, LR, CR, VB）を指定します。</param>
    /// <param name="address">対象の先頭アドレスを指定します。</param>
    /// <param name="count">点数（1〜16）を指定します。</param>
    /// <exception cref="DeviceAddressException">指定したアドレスが不正な場合にスローします。</exception>
    /// <exception cref="ArgumentOutOfRangeException">点数が 1〜16 の範囲外の場合にスローします。</exception>
    /// <exception cref="NotSupportedException">ビットデバイス以外を指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void ContinuousForceReset(Prefix prefix, string address, byte count)
    {
        new PlcCommandHandler<bool>().Execute(new ContinuousForceSetResetCommand(prefix, address, count, isSet: false), this);
    }

    /// <inheritdoc cref="ContinuousForceReset(Prefix, string, byte)"/>
    /// <summary>
    /// 連続強制リセット（非同期、コマンド: RSS）
    /// </summary>
    public async Task ContinuousForceResetAsync(Prefix prefix, string address, byte count)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new ContinuousForceSetResetCommand(prefix, address, count, isSet: false), this);
    }
}
