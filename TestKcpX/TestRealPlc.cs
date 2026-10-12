using KcpXLib.Enums;

namespace TestKcpX;

/// <summary>
/// 実機 PLC（KV-7500・TCP）を使った結合テスト。
/// </summary>
/// <remarks>
/// 実機の DM11000〜DM17199・MR50000〜MR53015を上書きします。<br/>
/// 通常の <c>dotnet test</c> では実行されず（Inconclusive）、環境変数 <c>KCPX_REAL_PLC=1</c> を指定した場合のみ実行します。
/// <code>
/// KCPX_REAL_PLC=1 dotnet test TestKcpX --filter TestCategory=RealPlc
/// </code>
/// 接続先は <c>KCPX_PLC_IP</c>（既定 192.168.12.89）/ <c>KCPX_PLC_PORT</c>（既定 8501）で変更できます。<br/>
/// タイマ／カウンタのテストは、ラダーで使用している番号を <c>KCPX_PLC_TIMER</c> / <c>KCPX_PLC_COUNTER</c>（既定 0）で、起動条件・カウント入力の MR を
/// <c>KCPX_PLC_TIMER_INPUT</c>（既定 002）/ <c>KCPX_PLC_COUNTER_INPUT</c>（既定 000）で指定します。<br/>
/// モード切替のテストは、PLC を一時的に PROGRAM モードにするため、<c>KCPX_PLC_MODE=1</c> を指定した場合のみ実行します。
/// </remarks>
[TestClass]
[TestCategory("RealPlc")]
public sealed class TestRealPlc : PlcIntegrationTestBase
{
    protected override string? ModeChangeEnableVar => "KCPX_PLC_MODE";

    protected override KcpXLib.KcpX Connect() =>
        ConnectIfEnabled("KCPX_REAL_PLC", "KCPX_PLC_IP", "192.168.12.89", "KCPX_PLC_PORT", 8501, false, PlcSeries.KV7000);
}

/// <summary>
/// 実機 PLC（KV-7500・UDP）を使った結合テスト。
/// </summary>
/// <remarks>
/// <see cref="TestRealPlc"/> と同じデバイスを上書きします。接続先も同じ環境変数で変更できます。
/// </remarks>
[TestClass]
[TestCategory("RealPlc")]
public sealed class TestRealPlcUdp : PlcIntegrationTestBase
{
    protected override KcpXLib.KcpX Connect() =>
        ConnectIfEnabled("KCPX_REAL_PLC", "KCPX_PLC_IP", "192.168.12.89", "KCPX_PLC_PORT", 8501, true, PlcSeries.KV7000);

    protected override string? ModeChangeEnableVar => "KCPX_PLC_MODE";
}
