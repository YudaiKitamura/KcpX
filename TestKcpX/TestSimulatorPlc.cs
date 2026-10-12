using KcpXLib;

namespace TestKcpX;

/// <summary>
/// KV STUDIO シミュレータ（<see cref="KcpXSimulator"/>）を使った結合テスト。
/// </summary>
/// <remarks>
/// シミュレータの DM11000〜DM17199・MR50000〜MR53015を上書きします。<br/>
/// 通常の <c>dotnet test</c> では実行されず（Inconclusive）、環境変数 <c>KCPX_SIM_PLC=1</c> を指定した場合のみ実行します。
/// <code>
/// KCPX_SIM_PLC=1 dotnet test TestKcpX --filter TestCategory=SimulatorPlc
/// </code>
/// 接続先は <c>KCPX_SIM_IP</c>（既定 172.18.0.2）/ <c>KCPX_SIM_PORT</c>（既定 8501）で変更できます。<br/>
/// 別の PC から接続する場合は、シミュレータ側で 8501 を 127.0.0.1 へ転送してください。<br/>
/// モニタは <see cref="KcpXSimulator"/> が非対応のため、<see cref="NotSupportedException"/> になることのみ確認します。
/// タイマ／カウンタのテストは <see cref="TestRealPlc"/> と同じ環境変数・ラダーを前提とします。
/// </remarks>
[TestClass]
[TestCategory("SimulatorPlc")]
public sealed class TestSimulatorPlc : PlcIntegrationTestBase
{
    protected override bool SupportsMonitor => false;

    protected override KcpXLib.KcpX Connect()
    {
        EnsureEnabled("KCPX_SIM_PLC");

        return new KcpXSimulator(Environment.GetEnvironmentVariable("KCPX_SIM_IP") ?? "172.18.0.2", GetEnvironmentInt("KCPX_SIM_PORT", 8501));
    }
}
