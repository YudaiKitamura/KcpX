namespace KcpXLib.Enums;

/// <summary>
/// 接続先 PLC のシリーズ
/// </summary>
/// <remarks>
/// 1 コマンドで読み書きできる点数の上限の判定に使用します。
/// </remarks>
public enum PlcSeries
{
    /// <summary>KV-8000 / KV-8000A</summary>
    KV8000,
    /// <summary>KV-7500 / KV-7300</summary>
    KV7000,
    /// <summary>KV-5500 / KV-5000 / KV-3000</summary>
    KV5000,
    /// <summary>KV Nano シリーズ（KV-NC1EP 経由）</summary>
    KVNano,
}
