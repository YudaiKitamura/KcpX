using KcpXLib.Commands;
using KcpXLib.Enums;
using KcpXLib.Exceptions;

namespace KcpXLib;

public partial class HostLink
{
    /// <summary>
    /// 動作モード確認（コマンド: ?M）
    /// </summary>
    /// <remarks>
    /// PLC の動作モードを読み出します。ラダーが登録されていない場合は <see cref="OperatingMode.Program"/> を返します。
    /// </remarks>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    /// <returns>動作モードを返します。</returns>
    public OperatingMode ReadOperatingMode()
    {
        return new PlcCommandHandler<OperatingMode>().Execute(new ModeReadCommand(), this);
    }

    /// <inheritdoc cref="ReadOperatingMode"/>
    /// <summary>
    /// 動作モード確認（非同期、コマンド: ?M）
    /// </summary>
    public async Task<OperatingMode> ReadOperatingModeAsync()
    {
        return await new PlcCommandHandler<OperatingMode>().ExecuteAsync(new ModeReadCommand(), this);
    }

    /// <summary>
    /// モード切替（コマンド: M0 / M1）
    /// </summary>
    /// <remarks>
    /// PLC を PROGRAM モード（M0）または RUN モード（M1）に切り替えます。<br/>
    /// RUN モードへの切替は、ラダーが登録されていない場合に E2、CPU エラーが解除されていない場合に E5 のエラーレスポンスになります。<br/>
    /// RUN モードへの切替は、応答を受信してから実際に RUN モードへ移行するまでに時間がかかる場合があります（KV-7500 で数十ミリ秒）。
    /// 移行を待つ場合は <see cref="ReadOperatingMode"/> で確認してください。
    /// </remarks>
    /// <param name="mode">切り替える動作モードを指定します。</param>
    /// <exception cref="ArgumentOutOfRangeException">定義されていない動作モードを指定した場合にスローします。</exception>
    /// <exception cref="ReceivePacketException">受信したレスポンスが不正な場合にスローします。</exception>
    /// <exception cref="HostLinkException">PLC からエラーレスポンスを受信した場合にスローします。</exception>
    public void ChangeOperatingMode(OperatingMode mode)
    {
        new PlcCommandHandler<bool>().Execute(new ModeChangeCommand(mode), this);
    }

    /// <inheritdoc cref="ChangeOperatingMode(OperatingMode)"/>
    /// <summary>
    /// モード切替（非同期、コマンド: M0 / M1）
    /// </summary>
    public async Task ChangeOperatingModeAsync(OperatingMode mode)
    {
        await new PlcCommandHandler<bool>().ExecuteAsync(new ModeChangeCommand(mode), this);
    }
}
