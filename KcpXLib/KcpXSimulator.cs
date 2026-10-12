using KcpXLib.Enums;
using KcpXLib.Interfaces;

namespace KcpXLib;

/// <summary>
/// KV STUDIO シミュレータ接続クラス
/// </summary>
/// <remarks>
/// KV STUDIO のシミュレータに、上位リンク（TCP）で接続するクラスです。（KV STUDIO Ver.12.41 で確認）<br/>
/// 対象 PLC は KV-8000 / KV-X500 のプロジェクトです。点数の上限は <see cref="PlcSeries.KV8000"/> として判定します。<br/>
/// シミュレータは 127.0.0.1 でのみ待ち受けるため、別の PC から接続する場合は、シミュレータ側の PC でポートフォワーディング（<c>netsh interface portproxy</c> など）を設定してください。<br/>
/// 接続できるシミュレータは 1 台のみです。（複数起動した場合は、最初に起動したシミュレータに接続されます）<br/>
/// 交信は TCP のみです。モニタ（コマンド: MBS / MBR / MWS / MWR）にはシミュレータが応答しないため、<see cref="KcpX.MonitorRegist(System.Action{MonitorBuilder})"/> は <see cref="NotSupportedException"/> をスローします。
/// </remarks>
public sealed class KcpXSimulator : KcpX
{
    /// <summary>
    /// インスタンス初期化
    /// </summary>
    /// <param name="ip">シミュレータが動作している PC の IP アドレスを指定します。（デフォルトは、<c>127.0.0.1</c>です。）</param>
    /// <param name="port">上位リンクのポート番号を指定します。（デフォルトは、<c>8501</c>です。）</param>
    /// <param name="timeoutMilliseconds">通信タイムアウト時間（ミリ秒）を指定します。<c>0</c> は無期限です。（デフォルトは、5 秒です。）</param>
    /// <exception cref="TimeoutException">指定したタイムアウト時間内に接続できなかった場合にスローします。</exception>
    /// <exception cref="System.Net.Sockets.SocketException">シミュレータが起動していない場合など、接続に失敗した場合にスローします。</exception>
    public KcpXSimulator(
        string ip = "127.0.0.1",
        int port = DEFAULT_PORT,
        ushort timeoutMilliseconds = 5000
    ) : base(ip, port, isUdp: false, timeoutMilliseconds, PlcSeries.KV8000)
    {
    }

    internal KcpXSimulator(IPlcTransport transport) : base(transport, PlcSeries.KV8000)
    {
    }

    private protected override void EnsureMonitorSupported() =>
        throw new NotSupportedException("The KV STUDIO simulator does not support monitor commands (MBS / MBR / MWS / MWR).");
}
